using LubeLogMCP.Helper;
using LubeLogMCP.Models;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Nodes;
using static LubeLogMCP.Helper.LubeLoggerApiClient;

namespace LubeLogMCP.MCP
{
    /// <summary>
    /// Update tools. LubeLogger's /update endpoints replace the whole record, so every tool here reads the current
    /// record, changes only the fields the caller supplied, and writes the whole record back. Files (attachments),
    /// extra fields and tags not mentioned by the caller are preserved.
    /// </summary>
    public partial class LubeLoggerMCP
    {
        private const string UpdateVehicleIdDescription = "id of the vehicle the record belongs to";
        private const string UpdateRecordIdDescription = "id of the record to update";
        private const string UpdateExtraFieldsDescription = "Optional extra fields to set; matched by name, other extra fields are kept. An empty value removes that field";
        private const string UpdateNotesDescription = "Optional: replaces the notes";
        private const string UpdateTagsDescription = "Optional: replaces all tags (space-separated); pass an empty string to clear them";

        [McpServerTool, Description("Updates a service record. Only the fields you pass are changed.")]
        public Task<string> UpdateServiceRecord(
            [Description(UpdateVehicleIdDescription)] int vehicleId,
            [Description(UpdateRecordIdDescription)] int id,
            [Description("New date")] DateTime? date = null,
            [Description("New odometer reading")] int? odometer = null,
            [Description("New description")] string? description = null,
            [Description("New total cost")] decimal? cost = null,
            [Description(UpdateNotesDescription)] string? notes = null,
            [Description(UpdateTagsDescription)] string? tags = null,
            [Description(UpdateExtraFieldsDescription)] List<ExtraField>? extraFields = null)
            => UpdateGenericRecordAsync("servicerecords", "service record", vehicleId, id, date, odometer, description, cost, notes, tags, extraFields);

        [McpServerTool, Description("Updates a repair record. Only the fields you pass are changed.")]
        public Task<string> UpdateRepairRecord(
            [Description(UpdateVehicleIdDescription)] int vehicleId,
            [Description(UpdateRecordIdDescription)] int id,
            [Description("New date")] DateTime? date = null,
            [Description("New odometer reading")] int? odometer = null,
            [Description("New description")] string? description = null,
            [Description("New total cost")] decimal? cost = null,
            [Description(UpdateNotesDescription)] string? notes = null,
            [Description(UpdateTagsDescription)] string? tags = null,
            [Description(UpdateExtraFieldsDescription)] List<ExtraField>? extraFields = null)
            => UpdateGenericRecordAsync("repairrecords", "repair record", vehicleId, id, date, odometer, description, cost, notes, tags, extraFields);

        [McpServerTool, Description("Updates an upgrade record. Only the fields you pass are changed.")]
        public Task<string> UpdateUpgradeRecord(
            [Description(UpdateVehicleIdDescription)] int vehicleId,
            [Description(UpdateRecordIdDescription)] int id,
            [Description("New date")] DateTime? date = null,
            [Description("New odometer reading")] int? odometer = null,
            [Description("New description")] string? description = null,
            [Description("New total cost")] decimal? cost = null,
            [Description(UpdateNotesDescription)] string? notes = null,
            [Description(UpdateTagsDescription)] string? tags = null,
            [Description(UpdateExtraFieldsDescription)] List<ExtraField>? extraFields = null)
            => UpdateGenericRecordAsync("upgraderecords", "upgrade record", vehicleId, id, date, odometer, description, cost, notes, tags, extraFields);

        [McpServerTool, Description("Updates a fuel (gas) record. Only the fields you pass are changed. For electric vehicles, volume (energy added) must always be passed.")]
        public async Task<string> UpdateFuelRecord(
            [Description(UpdateVehicleIdDescription)] int vehicleId,
            [Description(UpdateRecordIdDescription)] int id,
            [Description("New date")] DateTime? date = null,
            [Description("New odometer reading")] int? odometer = null,
            [Description("New volume of fuel (or energy for EVs). Required for electric vehicles.")] decimal? volume = null,
            [Description("New total cost")] decimal? cost = null,
            [Description("Whether it was filled completely")] bool? fillToFull = null,
            [Description("Whether a previous fuel up was missed")] bool? missedFuelUp = null,
            [Description("New starting state of charge (EVs)")] int? startingSoc = null,
            [Description("New ending state of charge (EVs)")] int? endingSoc = null,
            [Description(UpdateNotesDescription)] string? notes = null,
            [Description(UpdateTagsDescription)] string? tags = null,
            [Description(UpdateExtraFieldsDescription)] List<ExtraField>? extraFields = null)
        {
            if (!volume.HasValue)
            {
                // For EVs, LubeLogger's gas record list reports computed consumption instead of the stored amount,
                // so writing it back would overwrite the stored value. Make the caller supply it.
                var electric = await IsVehicleElectricAsync(vehicleId);
                if (electric == null)
                {
                    return $"Error: could not look up vehicle {vehicleId} to check whether it is electric.";
                }
                if (electric.Value)
                {
                    return "Error: vehicle is electric. LubeLogger's API doesn't return the stored energy amount for EV charging records, so pass volume (the amount originally recorded, or the corrected amount) when updating.";
                }
            }
            return await _api.UpdateRecordAsync("gasrecords", "fuel record", vehicleId, id, record =>
            {
                SetIfProvided(record, "date", date);
                SetIfProvided(record, "odometer", odometer);
                SetIfProvided(record, "fuelConsumed", volume);
                SetIfProvided(record, "cost", cost);
                SetIfProvided(record, "isFillToFull", fillToFull);
                SetIfProvided(record, "missedFuelUp", missedFuelUp);
                SetIfProvided(record, "startingSoc", startingSoc);
                SetIfProvided(record, "endingSoc", endingSoc);
                SetIfProvided(record, "notes", notes);
                SetIfProvided(record, "tags", tags);
                MergeExtraFields(record, extraFields);
            });
        }

        [McpServerTool, Description("Updates an odometer record. Only the fields you pass are changed.")]
        public Task<string> UpdateOdometerRecord(
            [Description(UpdateVehicleIdDescription)] int vehicleId,
            [Description(UpdateRecordIdDescription)] int id,
            [Description("New date")] DateTime? date = null,
            [Description("New initial odometer reading (start of the distance this record covers)")] int? initialOdometer = null,
            [Description("New odometer reading")] int? odometer = null,
            [Description("Optional: replaces the equipped equipment ids; pass an empty list to clear")] List<int>? equipmentRecordIds = null,
            [Description(UpdateNotesDescription)] string? notes = null,
            [Description(UpdateTagsDescription)] string? tags = null,
            [Description(UpdateExtraFieldsDescription)] List<ExtraField>? extraFields = null)
            => _api.UpdateRecordAsync("odometerrecords", "odometer record", vehicleId, id, record =>
            {
                SetIfProvided(record, "date", date);
                SetIfProvided(record, "initialOdometer", initialOdometer);
                SetIfProvided(record, "odometer", odometer);
                if (equipmentRecordIds != null)
                {
                    record["equipmentRecordId"] = string.Join(' ', equipmentRecordIds);
                }
                SetIfProvided(record, "notes", notes);
                SetIfProvided(record, "tags", tags);
                MergeExtraFields(record, extraFields);
            });

        [McpServerTool, Description("Updates a supply record. Use vehicleId 0 for shop supplies. Only the fields you pass are changed.")]
        public Task<string> UpdateSupplyRecord(
            [Description("id of the vehicle the record belongs to, or 0 for shop supplies")] int vehicleId,
            [Description(UpdateRecordIdDescription)] int id,
            [Description("New date")] DateTime? date = null,
            [Description("New description")] string? description = null,
            [Description("New quantity")] decimal? quantity = null,
            [Description("New cost")] decimal? cost = null,
            [Description("New part number")] string? partNumber = null,
            [Description("New part supplier")] string? partSupplier = null,
            [Description(UpdateNotesDescription)] string? notes = null,
            [Description(UpdateTagsDescription)] string? tags = null,
            [Description(UpdateExtraFieldsDescription)] List<ExtraField>? extraFields = null)
            => _api.UpdateRecordAsync("supplyrecords", "supply record", vehicleId, id, record =>
            {
                SetIfProvided(record, "date", date);
                SetIfProvided(record, "description", description);
                SetIfProvided(record, "partQuantity", quantity);
                SetIfProvided(record, "cost", cost);
                SetIfProvided(record, "partNumber", partNumber);
                SetIfProvided(record, "partSupplier", partSupplier);
                SetIfProvided(record, "notes", notes);
                SetIfProvided(record, "tags", tags);
                MergeExtraFields(record, extraFields);
            });

        [McpServerTool, Description("Updates a tax record. Only the fields you pass are changed.")]
        public Task<string> UpdateTaxRecord(
            [Description(UpdateVehicleIdDescription)] int vehicleId,
            [Description(UpdateRecordIdDescription)] int id,
            [Description("New date")] DateTime? date = null,
            [Description("New description")] string? description = null,
            [Description("New cost")] decimal? cost = null,
            [Description(UpdateNotesDescription)] string? notes = null,
            [Description(UpdateTagsDescription)] string? tags = null,
            [Description(UpdateExtraFieldsDescription)] List<ExtraField>? extraFields = null)
            => _api.UpdateRecordAsync("taxrecords", "tax record", vehicleId, id, record =>
            {
                SetIfProvided(record, "date", date);
                SetIfProvided(record, "description", description);
                SetIfProvided(record, "cost", cost);
                SetIfProvided(record, "notes", notes);
                SetIfProvided(record, "tags", tags);
                MergeExtraFields(record, extraFields);
            });

        [McpServerTool, Description("Updates a note. Only the fields you pass are changed.")]
        public Task<string> UpdateNote(
            [Description(UpdateVehicleIdDescription)] int vehicleId,
            [Description("id of the note to update")] int id,
            [Description("New title")] string? description = null,
            [Description("New body text")] string? noteText = null,
            [Description("Pin or unpin the note")] bool? pinned = null,
            [Description(UpdateTagsDescription)] string? tags = null,
            [Description(UpdateExtraFieldsDescription)] List<ExtraField>? extraFields = null)
            => _api.UpdateRecordAsync("notes", "note", vehicleId, id, record =>
            {
                SetIfProvided(record, "description", description);
                SetIfProvided(record, "noteText", noteText);
                SetIfProvided(record, "pinned", pinned);
                SetIfProvided(record, "tags", tags);
                MergeExtraFields(record, extraFields);
            });

        [McpServerTool, Description("Updates a planner record. Only the fields you pass are changed. Plans marked Done can't be updated through the API, and progress can't be set to Done.")]
        public Task<string> UpdatePlanRecord(
            [Description(UpdateVehicleIdDescription)] int vehicleId,
            [Description("id of the plan to update")] int id,
            [Description("New description")] string? description = null,
            [Description("New estimated cost")] decimal? cost = null,
            [Description("New record type the plan becomes when done")] PlanType? type = null,
            [Description("New priority")] PlanPriority? priority = null,
            [Description("New progress")] PlanProgress? progress = null,
            [Description(UpdateNotesDescription)] string? notes = null,
            [Description(UpdateExtraFieldsDescription)] List<ExtraField>? extraFields = null)
            => _api.UpdateRecordAsync("planrecords", "plan", vehicleId, id, record =>
            {
                SetIfProvided(record, "description", description);
                SetIfProvided(record, "cost", cost);
                SetIfProvided(record, "type", type?.ToString());
                SetIfProvided(record, "priority", priority?.ToString());
                SetIfProvided(record, "progress", progress?.ToString());
                SetIfProvided(record, "notes", notes);
                MergeExtraFields(record, extraFields);
            });

        [McpServerTool, Description("Updates a reminder. Only the fields you pass are changed; recurring settings configured in the UI are kept. Metric Date needs a due date, Odometer a due odometer, Both needs both.")]
        public Task<string> UpdateReminder(
            [Description(UpdateVehicleIdDescription)] int vehicleId,
            [Description("id of the reminder to update")] int id,
            [Description("New description")] string? description = null,
            [Description("New metric: Date, Odometer or Both")] ReminderMetric? metric = null,
            [Description("New due date")] DateTime? dueDate = null,
            [Description("New due odometer reading")] int? dueOdometer = null,
            [Description(UpdateNotesDescription)] string? notes = null,
            [Description(UpdateTagsDescription)] string? tags = null)
            => _api.UpdateRecordAsync("reminders", "reminder", vehicleId, id, record =>
            {
                SetIfProvided(record, "description", description);
                SetIfProvided(record, "metric", metric?.ToString());
                SetIfProvided(record, "dueDate", dueDate);
                SetIfProvided(record, "dueOdometer", dueOdometer);
                SetIfProvided(record, "notes", notes);
                SetIfProvided(record, "tags", tags);
            });

        [McpServerTool, Description("Updates an equipment record. Only the fields you pass are changed.")]
        public Task<string> UpdateEquipmentRecord(
            [Description(UpdateVehicleIdDescription)] int vehicleId,
            [Description(UpdateRecordIdDescription)] int id,
            [Description("New description")] string? description = null,
            [Description("Whether it is currently equipped")] bool? isEquipped = null,
            [Description(UpdateNotesDescription)] string? notes = null,
            [Description(UpdateTagsDescription)] string? tags = null,
            [Description(UpdateExtraFieldsDescription)] List<ExtraField>? extraFields = null)
            => _api.UpdateRecordAsync("equipmentrecords", "equipment record", vehicleId, id, record =>
            {
                SetIfProvided(record, "description", description);
                SetIfProvided(record, "isEquipped", isEquipped);
                SetIfProvided(record, "notes", notes);
                SetIfProvided(record, "tags", tags);
                MergeExtraFields(record, extraFields);
            });

        [McpServerTool, Description("Updates a vehicle's details (year, make, model, license plate, tags, extra fields such as VIN or trim). Only the fields you pass are changed. Fuel type can't be changed through this tool.")]
        public async Task<string> UpdateVehicle(
            [Description("id of the vehicle to update")] int vehicleId,
            [Description("New model year")] int? year = null,
            [Description("New make")] string? make = null,
            [Description("New model")] string? model = null,
            [Description("New license plate")] string? licensePlate = null,
            [Description("Field used to identify the vehicle: LicensePlate, or the name of an extra field (e.g. VIN)")] string? identifier = null,
            [Description("Whether the vehicle tracks engine hours instead of distance")] bool? useEngineHours = null,
            [Description("Whether odometer readings are optional")] bool? odometerOptional = null,
            [Description(UpdateTagsDescription)] string? tags = null,
            [Description(UpdateExtraFieldsDescription)] List<ExtraField>? extraFields = null)
        {
            var current = await _api.GetAsync("/api/vehicles");
            if (!current.Success)
            {
                return current.ToToolResult();
            }
            JsonObject? vehicle;
            try
            {
                vehicle = (JsonNode.Parse(current.Body) as JsonArray)?
                    .OfType<JsonObject>()
                    .FirstOrDefault(x => x["id"]?.GetValue<int>() == vehicleId);
            }
            catch (Exception ex) when (ex is JsonException or InvalidOperationException or FormatException)
            {
                return $"Error: unexpected response from LubeLogger while reading vehicles: {ex.Message}";
            }
            if (vehicle == null)
            {
                return $"Error: no vehicle with id {vehicleId} found.";
            }
            // GET /api/vehicles returns the internal Vehicle model; /api/vehicles/update takes VehicleImportModel.
            bool isElectric = vehicle["isElectric"]?.GetValue<bool>() ?? false;
            bool isDiesel = vehicle["isDiesel"]?.GetValue<bool>() ?? false;
            var existingTags = vehicle["tags"] as JsonArray;
            var body = new JsonObject
            {
                ["id"] = vehicleId,
                ["year"] = vehicle["year"]?.DeepClone(),
                ["make"] = vehicle["make"]?.DeepClone(),
                ["model"] = vehicle["model"]?.DeepClone(),
                ["licensePlate"] = vehicle["licensePlate"]?.DeepClone(),
                ["identifier"] = vehicle["vehicleIdentifier"]?.DeepClone() ?? "LicensePlate",
                ["fuelType"] = isElectric ? "Electric" : isDiesel ? "Diesel" : "Gasoline",
                ["useEngineHours"] = vehicle["useHours"]?.DeepClone() ?? false,
                ["odometerOptional"] = vehicle["odometerOptional"]?.DeepClone() ?? false,
                ["tags"] = existingTags == null ? string.Empty : string.Join(' ', existingTags.Select(x => x?.ToString())),
                ["extraFields"] = vehicle["extraFields"]?.DeepClone() ?? new JsonArray()
            };
            SetIfProvided(body, "year", year);
            SetIfProvided(body, "make", make);
            SetIfProvided(body, "model", model);
            SetIfProvided(body, "licensePlate", licensePlate);
            SetIfProvided(body, "identifier", identifier);
            SetIfProvided(body, "useEngineHours", useEngineHours);
            SetIfProvided(body, "odometerOptional", odometerOptional);
            SetIfProvided(body, "tags", tags);
            MergeExtraFields(body, extraFields);
            var result = await _api.SendJsonAsync(HttpMethod.Put, "/api/vehicles/update", body);
            return result.ToToolResult();
        }

        [McpServerTool, Description("Recalculates the distance traveled on every odometer record of a vehicle (initial odometer from the previous record). Use after correcting odometer records.")]
        public async Task<string> RecalculateOdometerRecords(
            [Description("id of the vehicle")] int vehicleId)
        {
            var result = await _api.SendAsync(HttpMethod.Put, "/api/vehicle/odometerrecords/recalculate" + Query(("vehicleId", vehicleId.ToString())));
            return result.ToToolResult();
        }

        private Task<string> UpdateGenericRecordAsync(string recordPath, string recordLabel, int vehicleId, int id,
            DateTime? date, int? odometer, string? description, decimal? cost, string? notes, string? tags, List<ExtraField>? extraFields)
            => _api.UpdateRecordAsync(recordPath, recordLabel, vehicleId, id, record =>
            {
                SetIfProvided(record, "date", date);
                SetIfProvided(record, "odometer", odometer);
                SetIfProvided(record, "description", description);
                SetIfProvided(record, "cost", cost);
                SetIfProvided(record, "notes", notes);
                SetIfProvided(record, "tags", tags);
                MergeExtraFields(record, extraFields);
            });

        /// <summary>
        /// Returns whether the vehicle is electric, or null if it couldn't be determined.
        /// </summary>
        private async Task<bool?> IsVehicleElectricAsync(int vehicleId)
        {
            var response = await _api.GetAsync("/api/vehicles");
            if (!response.Success)
            {
                return null;
            }
            try
            {
                var vehicles = JsonSerializer.Deserialize<List<Vehicle>>(response.Body, JsonOptions);
                return vehicles?.FirstOrDefault(x => x.Id == vehicleId)?.IsElectric;
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
