# Plan: full LubeLogger API coverage

Working plan for extending this fork of `hargata/lubelog_mcp` so the MCP covers
LubeLogger's whole REST API, not just the add tools. Written from LubeLogger
**1.7.5** source (`hargata/lubelog`, `Controllers/API/*.cs`,
`Models/Shared/ImportModel.cs`). Delete this file before opening any PR upstream.

## Goals

- Read tools for every record type, plus reminders and vehicle stats.
- Update tools for every record type, so a mistake can be fixed without the UI.
- Delete tools, **disabled by default** behind an env flag.
- Keep upstream's conventions (one tool per operation per record type,
  `[McpServerTool]` + `[Description]`, auth passthrough) so the work stays
  mergeable and can be offered back to hargata as PRs.
- Leave transport, auth model, `/api/mcp` path and port 8080 unchanged, so it
  stays a drop-in replacement for the current image.

## Dev setup (local)

- .NET 10 SDK (`brew install dotnet`), then `dotnet build` from the repo root.
- Run: `LUBELOG_INSTANCE=https://<lubelogger-host> dotnet run --project LubeLogMCP`
- Test calls send `x-api-key` from a shell variable or a git-ignored `.env`. Never
  commit a key or homelab hostnames/IPs; this repo is public.
- Use a **Viewer** key while building read tools. Use an **Editor** key only for
  write tests, against a throwaway test vehicle.
- Smoke test:
  ```bash
  curl -s -X POST http://localhost:5000/api/mcp \
    -H "x-api-key: $LUBELOG_KEY" -H 'Content-Type: application/json' \
    -H 'Accept: application/json, text/event-stream' \
    -d '{"jsonrpc":"2.0","id":1,"method":"tools/list"}'
  ```
  (Check the port `dotnet run` prints; the container uses 8080.)

## Implementation approach

1. **Shared HTTP helper.** Every existing tool repeats the
   request / `AddAuthHeaders` / `SendAsync(...).Result` / catch block. Add private
   helpers (`GetAsync(path)`, `SendJsonAsync(method, path, JsonNode body)`) and
   use them in new tools. While there, replace the `.Result` calls with `await`:
   they block a thread for no reason. Refactoring the existing tools is optional;
   keep that diff separate if it's offered upstream.
2. **Reads return raw JSON with the `culture-invariant: true` header.** That makes
   LubeLogger emit ISO dates (`yyyy-MM-dd`) and invariant decimals instead of
   server-locale `ToShortDateString()` output. Returning raw JSON avoids modelling
   every export type in C#. Don't deserialize into typed models unless a tool
   needs to filter or reshape the data.
3. **Updates are read-modify-write.** Every `/update` endpoint replaces the whole
   record, including `files`, `extraFields` and `tags`. Sending only the changed
   fields wipes attachments and tags. So each update tool should:
   - GET `/api/vehicle/<type>?vehicleId=X&id=Y` (with the invariant header),
   - take the single result as a `JsonObject`,
   - overwrite only the fields the caller supplied (nullable tool parameters),
   - PUT the whole object back as JSON.

   The update tools therefore take `vehicleId` as well as the record `id`.
4. **Write bodies are JSON.** All add/update endpoints have a
   `[Consumes("application/json")]` overload. The models are string-typed, with
   converters that also accept numbers and bools, so JSON strings are always
   safe. `AddVehicleRecord` upstream uses form encoding; leave it as it is.
5. **Delete gating.** Register delete tools only when
   `LUBELOG_ENABLE_DELETE=true`, so they don't appear in `tools/list` at all
   otherwise. Simplest: put them in a separate `[McpServerToolType]` class and
   register it conditionally in `Program.cs` instead of relying on
   `WithToolsFromAssembly()` for it. LubeLogger enforces permissions anyway: a
   non-Manager key gets 401/403.

## API reference (from source)

Common query parameters on list endpoints (`MethodParameter`): `vehicleId`
(required on per-vehicle routes), `id`, `startDate`, `endDate` (date strings),
`tags` (space-separated). `/all` variants drop `vehicleId` and span every vehicle
the key can access.

| Type | List | Add (required) | Update (required) | Delete |
|---|---|---|---|---|
| Service | `GET /api/vehicle/servicerecords` | `POST .../servicerecords/add?vehicleId=` date, description, odometer, cost | `PUT .../servicerecords/update` id + same | `DELETE .../servicerecords/delete?id=` |
| Repair | `.../repairrecords` | same as service | same | same pattern |
| Upgrade | `.../upgraderecords` | same as service | same | same pattern |
| Gas | `.../gasrecords` | date, odometer, fuelConsumed, isFillToFull, missedFuelUp, cost | id + same | same pattern |
| Odometer | `.../odometerrecords` (+ `/latest`) | date, odometer (optional `equipmentRecordId`, space-separated ids) | id, date, **initialOdometer**, odometer | same pattern |
| Supply | `.../supplyrecords` (vehicleId=0 for shop supplies) | date, description, partQuantity, cost | id + same | same pattern |
| Tax | `.../taxrecords` (+ `/check`) | date, description, cost | id + same | same pattern |
| Note | `.../notes` | description, noteText (optional `pinned`) | id + same | same pattern |
| Plan | `.../planrecords` | description, cost, type, priority, progress | id + same | same pattern |
| Reminder | `.../reminders` | description, metric + dueDate and/or dueOdometer | id + same | same pattern |
| Equipment | `.../equipmentrecords` | description, isEquipped | id + same | same pattern |

Enums and validation:
- **Plan:** `type` ∈ ServiceRecord, RepairRecord, UpgradeRecord. `priority` ∈
  Critical, Normal, Low. `progress` ∈ Backlog, InProgress, Testing. `Done` is
  rejected through the API.
- **Reminder:** `metric` ∈ Date, Odometer, Both. Date needs `dueDate`, Odometer
  needs `dueOdometer`, Both needs both.
- **Reminder list** accepts an `urgencies` filter (NotUrgent, Urgent, VeryUrgent,
  PastDue; default is all). Each result includes `urgency`, `dueDate`,
  `dueOdometer`, `dueDays` and `dueDistance`, computed against the vehicle's max
  odometer.

Other endpoints worth a tool:
- `GET /api/vehicle/info?vehicleId=` (vehicleId optional, which returns all
  vehicles): reminder counts by urgency, `nextReminder`, per-type record counts
  and costs, `lastReportedOdometer`, plan counts. The best single tool for
  "what's due / what's the state of this car".
- `GET /api/vehicle/adjustedodometer`, `POST /api/vehicle/odometerrecords/recalculate`.
- `GET /api/calendar` (reminders as iCal). Probably skip; it isn't useful to an LLM.
- `PUT /api/vehicles/update`: update vehicle details (incl. extra fields like
  VIN/Trim).

Known API limitations, to document rather than work around:
- **Reminders created through the API are one-off.** The add/update models have
  no recurring-interval fields, so recurring reminders can only be set up in the
  UI. The API can still read and update them.
- `/api/vehicles/delete`, `/api/makebackup`, `/api/cleanup` (root-only),
  `/api/demo/restore`, `/api/documents/upload`: out of scope.

## Tool list

Keep `PascalCase` method names; the SDK snake_cases them (`get_reminders`).
Match the names Barrow1990's fork already uses (`GetReminders`,
`GetOdometerRecords`) so the two forks don't diverge.

**Read:** GetVehicleInfo, GetServiceRecords, GetRepairRecords,
GetUpgradeRecords, GetFuelRecords, GetOdometerRecords, GetSupplyRecords,
GetTaxRecords, GetNotes, GetPlanRecords, GetReminders, GetEquipment (all, not
just equipped; keep upstream's GetEquippedEquipment as is).
- Each takes `vehicleId` plus optional `startDate`, `endDate`, `tags`.
- GetReminders also takes optional urgencies.

**Add (new):** AddTaxRecord, AddNote, AddPlanRecord, AddReminder,
AddEquipmentRecord.
- Existing add tools stay as they are.
- Optional: give upstream's add tools an optional `notes` and `tags` parameter.
  Today there's no way to set either.

**Update:** UpdateServiceRecord, UpdateRepairRecord, UpdateUpgradeRecord,
UpdateFuelRecord, UpdateOdometerRecord, UpdateSupplyRecord, UpdateTaxRecord,
UpdateNote, UpdatePlanRecord, UpdateReminder, UpdateEquipmentRecord,
UpdateVehicle. All read-modify-write.

**Delete (gated):** Delete<Type>Record for each type, plus DeleteReminder and
DeleteNote.

## Hardening (separate commit)

- `Program.cs`: drop the `AddCors` / `UseCors` allow-any-origin policy. A
  server-to-server MCP behind a reverse proxy doesn't need CORS.
- `Program.cs`: drop `UseHttpsRedirection()`. TLS terminates at the reverse proxy.
- Status tool: "Auth Not Configured" only reflects the `LUBELOG_USER/PASS`
  fallback. Report header passthrough ("using client-supplied API key") so the
  message isn't misleading.
- Bump `StaticHelper.VersionNumber` (e.g. `0.2.0-fork`).

## CI

The upstream workflow pushes images using hargata's registry secrets, so it fails
in this fork. Disable or replace it:
- `build.yml`: `dotnet build -c Release` on push and PR (compile check).
- Optional `docker.yml`: build and push to `ghcr.io/klynn7/lubelogger_mcp`
  using the repo's `GITHUB_TOKEN`, so the compose file can use this fork's image.

## Verification checklist

- [ ] `dotnet build` clean, no new warnings
- [ ] `tools/list` shows read/add/update tools; delete tools absent without the flag
- [ ] Every Get* tool returns ISO dates against the live instance
- [ ] Update on a test record keeps its existing tags, extra fields and attachments
- [ ] AddReminder with each metric (Date / Odometer / Both) succeeds;
      invalid combinations surface LubeLogger's error text
- [ ] A Viewer key gets a clean error (not an exception string) on any write
- [ ] README updated with the tool list, `LUBELOG_ENABLE_DELETE`, and the
      one-off-reminder limitation
