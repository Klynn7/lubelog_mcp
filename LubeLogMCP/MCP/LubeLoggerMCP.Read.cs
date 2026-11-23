using LubeLogMCP.Helper;
using LubeLogMCP.Models;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace LubeLogMCP.MCP
{
    /// <summary>
    /// Read tools. All return LubeLogger's JSON as-is, requested culture-invariant (ISO dates, plain numbers).
    /// </summary>
    public partial class LubeLoggerMCP
    {
        private const string VehicleIdDescription = "id of the vehicle";
        private const string RecordIdFilterDescription = "Optional: only return the record with this id";
        private const string StartDateDescription = "Optional: only records on or after this date";
        private const string EndDateDescription = "Optional: only records on or before this date";
        private const string TagsFilterDescription = "Optional: space-separated tags; returns records that have any of them";

        [McpServerTool(ReadOnly = true, OpenWorld = false, Title = "Get vehicle info"), Description("Gets a summary of a vehicle's state: reminder counts by urgency, the next reminder due, record counts and costs per type, last reported odometer and plan counts. Omit vehicleId to get every vehicle. Best starting point for 'what's due' questions.")]
        public async Task<string> GetVehicleInfo(
            [Description("Optional id of the vehicle; omit for all vehicles")] int? vehicleId = null)
        {
            var response = await _api.GetAsync("/api/vehicle/info" + LubeLoggerApiClient.Query(("vehicleId", vehicleId?.ToString())));
            return response.ToToolResult();
        }

        [McpServerTool(ReadOnly = true, OpenWorld = false, Title = "Get service records"), Description("Gets service records for a vehicle.")]
        public Task<string> GetServiceRecords(
            [Description(VehicleIdDescription)] int vehicleId,
            [Description(StartDateDescription)] DateTime? startDate = null,
            [Description(EndDateDescription)] DateTime? endDate = null,
            [Description(TagsFilterDescription)] string? tags = null,
            [Description(RecordIdFilterDescription)] int? id = null)
            => _api.ListRecordsAsync("servicerecords", vehicleId, id, startDate, endDate, tags);

        [McpServerTool(ReadOnly = true, OpenWorld = false, Title = "Get repair records"), Description("Gets repair records for a vehicle.")]
        public Task<string> GetRepairRecords(
            [Description(VehicleIdDescription)] int vehicleId,
            [Description(StartDateDescription)] DateTime? startDate = null,
            [Description(EndDateDescription)] DateTime? endDate = null,
            [Description(TagsFilterDescription)] string? tags = null,
            [Description(RecordIdFilterDescription)] int? id = null)
            => _api.ListRecordsAsync("repairrecords", vehicleId, id, startDate, endDate, tags);

        [McpServerTool(ReadOnly = true, OpenWorld = false, Title = "Get upgrade records"), Description("Gets upgrade records for a vehicle.")]
        public Task<string> GetUpgradeRecords(
            [Description(VehicleIdDescription)] int vehicleId,
            [Description(StartDateDescription)] DateTime? startDate = null,
            [Description(EndDateDescription)] DateTime? endDate = null,
            [Description(TagsFilterDescription)] string? tags = null,
            [Description(RecordIdFilterDescription)] int? id = null)
            => _api.ListRecordsAsync("upgraderecords", vehicleId, id, startDate, endDate, tags);

        [McpServerTool(ReadOnly = true, OpenWorld = false, Title = "Get fuel records"), Description("Gets fuel (gas) records for a vehicle, including computed fuel economy.")]
        public Task<string> GetFuelRecords(
            [Description(VehicleIdDescription)] int vehicleId,
            [Description(StartDateDescription)] DateTime? startDate = null,
            [Description(EndDateDescription)] DateTime? endDate = null,
            [Description(TagsFilterDescription)] string? tags = null,
            [Description(RecordIdFilterDescription)] int? id = null)
            => _api.ListRecordsAsync("gasrecords", vehicleId, id, startDate, endDate, tags);

        [McpServerTool(ReadOnly = true, OpenWorld = false, Title = "Get odometer records"), Description("Gets odometer records for a vehicle.")]
        public Task<string> GetOdometerRecords(
            [Description(VehicleIdDescription)] int vehicleId,
            [Description(StartDateDescription)] DateTime? startDate = null,
            [Description(EndDateDescription)] DateTime? endDate = null,
            [Description(TagsFilterDescription)] string? tags = null,
            [Description(RecordIdFilterDescription)] int? id = null)
            => _api.ListRecordsAsync("odometerrecords", vehicleId, id, startDate, endDate, tags);

        [McpServerTool(ReadOnly = true, OpenWorld = false, Title = "Get supply records"), Description("Gets supply records for a vehicle. Use vehicleId 0 for shop supplies (if shop supplies are enabled on the server).")]
        public Task<string> GetSupplyRecords(
            [Description("id of the vehicle, or 0 for shop supplies")] int vehicleId,
            [Description(StartDateDescription)] DateTime? startDate = null,
            [Description(EndDateDescription)] DateTime? endDate = null,
            [Description(TagsFilterDescription)] string? tags = null,
            [Description(RecordIdFilterDescription)] int? id = null)
            => _api.ListRecordsAsync("supplyrecords", vehicleId, id, startDate, endDate, tags);

        [McpServerTool(ReadOnly = true, OpenWorld = false, Title = "Get tax records"), Description("Gets tax records (registration, fees, etc.) for a vehicle.")]
        public Task<string> GetTaxRecords(
            [Description(VehicleIdDescription)] int vehicleId,
            [Description(StartDateDescription)] DateTime? startDate = null,
            [Description(EndDateDescription)] DateTime? endDate = null,
            [Description(TagsFilterDescription)] string? tags = null,
            [Description(RecordIdFilterDescription)] int? id = null)
            => _api.ListRecordsAsync("taxrecords", vehicleId, id, startDate, endDate, tags);

        [McpServerTool(ReadOnly = true, OpenWorld = false, Title = "Get notes"), Description("Gets notes for a vehicle.")]
        public async Task<string> GetNotes(
            [Description(VehicleIdDescription)] int vehicleId,
            [Description(TagsFilterDescription)] string? tags = null,
            [Description(RecordIdFilterDescription)] int? id = null)
        {
            // Notes have no date, so LubeLogger ignores date filters for them.
            return await _api.ListRecordsAsync("notes", vehicleId, id, null, null, tags);
        }

        [McpServerTool(ReadOnly = true, OpenWorld = false, Title = "Get plan records"), Description("Gets planner records (planned service/repair/upgrade work) for a vehicle, with type, priority and progress.")]
        public async Task<string> GetPlanRecords(
            [Description(VehicleIdDescription)] int vehicleId,
            [Description("Optional: only plans created on or after this date")] DateTime? startDate = null,
            [Description("Optional: only plans created on or before this date")] DateTime? endDate = null,
            [Description(RecordIdFilterDescription)] int? id = null)
        {
            return await _api.ListRecordsAsync("planrecords", vehicleId, id, startDate, endDate, null);
        }

        [McpServerTool(ReadOnly = true, OpenWorld = false, Title = "Get reminders"), Description("Gets reminders for a vehicle. Each result includes urgency, metric, dueDate, dueOdometer, dueDays and dueDistance (computed against the vehicle's highest odometer reading).")]
        public async Task<string> GetReminders(
            [Description(VehicleIdDescription)] int vehicleId,
            [Description("Optional: only reminders with these urgencies (NotUrgent, Urgent, VeryUrgent, PastDue). Default is all.")] List<ReminderUrgency>? urgencies = null,
            [Description(TagsFilterDescription)] string? tags = null,
            [Description(RecordIdFilterDescription)] int? id = null)
        {
            var parameters = new List<(string, string?)>
            {
                ("vehicleId", vehicleId.ToString()),
                ("id", id?.ToString()),
                ("tags", tags)
            };
            foreach (var urgency in urgencies ?? new List<ReminderUrgency>())
            {
                parameters.Add(("urgencies", urgency.ToString()));
            }
            var response = await _api.GetAsync("/api/vehicle/reminders" + LubeLoggerApiClient.Query(parameters.ToArray()));
            return response.ToToolResult();
        }

        [McpServerTool(ReadOnly = true, OpenWorld = false, Title = "Get equipment"), Description("Gets all equipment for a vehicle (equipped or not), with distance traveled. See GetEquippedEquipment for only the equipped items.")]
        public async Task<string> GetEquipment(
            [Description(VehicleIdDescription)] int vehicleId,
            [Description(TagsFilterDescription)] string? tags = null,
            [Description(RecordIdFilterDescription)] int? id = null)
        {
            return await _api.ListRecordsAsync("equipmentrecords", vehicleId, id, null, null, tags);
        }

        [McpServerTool(ReadOnly = true, OpenWorld = false, Title = "Get adjusted odometer"), Description("Converts a raw odometer reading using the vehicle's odometer adjustment (difference and multiplier) settings. Returns the reading unchanged if the vehicle has no adjustment.")]
        public async Task<string> GetAdjustedOdometer(
            [Description(VehicleIdDescription)] int vehicleId,
            [Description("Raw odometer reading to convert")] int odometer)
        {
            var response = await _api.GetAsync("/api/vehicle/adjustedodometer" + LubeLoggerApiClient.Query(
                ("vehicleId", vehicleId.ToString()),
                ("odometer", odometer.ToString())));
            return response.ToToolResult();
        }
    }
}
