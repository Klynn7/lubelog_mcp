# MCP Server for LubeLogger

<img width="735" height="306" alt="image" src="https://github.com/user-attachments/assets/85887c27-48aa-42ff-b9db-f5a3fe225a9a" />

This is a technical tool mainly for experimentation and exploration into AI-enabled features in LubeLogger. 

AI and MCP are still evolving technologies, implementations of this project is subject to break without prior notice.

## Pre-requisites
- AI agent with ability to call tools/external integrations(i.e.: Claude Desktop)
- Node.js(for npx)

## Getting Started

This MCP Server allows for authentication to be configured at the AI-Agent level(Header Auth) or at the MCP Server level(Local Auth).

Header Auth is more complex to set up but offers greater security, whereas Local Auth is easier to set up but allows users unfettered access to all vehicles in the instance.

For details on both configurations, see [Wiki](https://github.com/hargata/lubelog_mcp/wiki)

## Fork notes

This fork extends upstream to cover LubeLogger's whole REST API (written against LubeLogger 1.7.5): reads,
updates and (optionally) deletes for every record type, alongside upstream's add tools. Transport, auth,
the `/api/mcp` path and port 8080 are unchanged, so it is a drop-in replacement for the upstream image.

### Configuration

| Variable | Purpose |
|---|---|
| `LUBELOG_INSTANCE` | Base URL of the LubeLogger instance (required). |
| `LUBELOG_USER` / `LUBELOG_PASS` | Local Auth fallback, used only when the client sends no `Authorization`/`x-api-key` header or `apiKey` query parameter. |
| `LUBELOG_ENABLE_DELETE` | Set to `true` to register the delete tools. Off by default; without it they don't appear in `tools/list`. LubeLogger still requires a key with Delete permission. |

## Tools

**Status and vehicles:** `get_lube_logger_mcp_status`, `get_vehicles`, `get_vehicle_is_electric`, `get_vehicle_info`
(reminder counts, next reminder, record counts and costs, last odometer; omit `vehicleId` for all vehicles),
`add_vehicle_record`, `update_vehicle`, `get_extra_fields`.

**Read** (all take `vehicleId`; most also take optional `startDate`, `endDate`, `tags` and `id` filters; results are
LubeLogger's JSON with ISO dates):
`get_service_records`, `get_repair_records`, `get_upgrade_records`, `get_fuel_records`, `get_odometer_records`,
`get_latest_odometer`, `get_adjusted_odometer`, `get_supply_records` (`vehicleId` 0 for shop supplies), `get_tax_records`,
`get_notes`, `get_plan_records`, `get_reminders` (optional `urgencies` filter), `get_equipment`, `get_equipped_equipment`.

**Add:** `add_service_record`, `add_repair_record`, `add_upgrade_record`, `add_fuel_record`, `add_odometer_record`,
`add_supply_record`, `add_shop_supply_record`, `add_tax_record`, `add_note`, `add_plan_record`, `add_reminder`,
`add_equipment_record`. The add tools accept optional `notes` and `tags` (space-separated).

**Update:** `update_service_record`, `update_repair_record`, `update_upgrade_record`, `update_fuel_record`,
`update_odometer_record`, `update_supply_record`, `update_tax_record`, `update_note`, `update_plan_record`,
`update_reminder`, `update_equipment_record`, `update_vehicle`, plus `recalculate_odometer_records`.
Updates take the record's `vehicleId` and `id`. Only the fields you pass change: the tool reads the current record
and writes it back whole, so attachments, tags and extra fields you don't mention are kept. Extra fields are merged
by name.

**Delete** (only with `LUBELOG_ENABLE_DELETE=true`): `delete_service_record`, `delete_repair_record`,
`delete_upgrade_record`, `delete_fuel_record`, `delete_odometer_record`, `delete_supply_record`, `delete_tax_record`,
`delete_note`, `delete_plan_record`, `delete_reminder`, `delete_equipment_record`.

### Known limitations (LubeLogger API)

- **Reminders created through the API are one-off.** The API has no recurring-interval fields, so recurring
  reminders have to be set up in the LubeLogger UI. The API can still read and update them, and updates keep
  their recurring settings.
- Plans can't be set to `Done` through the API, and a plan that is already `Done` can't be updated.
- For electric vehicles the fuel record list reports computed consumption rather than the stored amount, so
  `update_fuel_record` requires `volume` for EVs.
- A vehicle's fuel type can't be changed through `update_vehicle`.
- Vehicle deletion, backups, cleanup and document upload are out of scope.

## Example Usage

<img width="760" height="700" alt="image" src="https://github.com/user-attachments/assets/a99f4570-adae-406c-914c-d580fcf3cce6" />

<img width="758" height="566" alt="image" src="https://github.com/user-attachments/assets/abf9ef37-7cfe-4701-b1b7-272e8a474841" />

<img width="735" height="561" alt="image" src="https://github.com/user-attachments/assets/a032512f-66c4-4a0a-8f98-54f7d28a623a" />
