using LubeLogMCP.Helper;
using LubeLogMCP.Models;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json.Nodes;

namespace LubeLogMCP.MCP
{
    /// <summary>
    /// Add tools for record types upstream doesn't cover. Bodies are JSON; numbers are sent as JSON numbers so
    /// LubeLogger parses them correctly regardless of the server's locale.
    /// </summary>
    public partial class LubeLoggerMCP
    {
        [McpServerTool(Destructive = false, Idempotent = false, OpenWorld = false, Title = "Add tax record"), Description("Adds a tax record (registration, inspection fee, toll pass, etc.).")]
        public async Task<string> AddTaxRecord(
            [Description("id of the vehicle")] int vehicleId,
            [Description("Date paid")] DateTime date,
            [Description("Description of the tax or fee")] string description,
            [Description("Total cost")] decimal cost,
            [Description("Any extra fields configured for taxrecord")] List<ExtraField>? extraFields = null,
            [Description("Optional notes")] string? notes = null,
            [Description("Optional space-separated tags")] string? tags = null)
        {
            var body = new JsonObject
            {
                ["date"] = LubeLoggerApiClient.FormatDate(date),
                ["description"] = description,
                ["cost"] = cost,
                ["notes"] = notes ?? string.Empty,
                ["tags"] = tags ?? string.Empty,
                ["extraFields"] = LubeLoggerApiClient.ToJsonArray(extraFields)
            };
            return await AddJsonRecordAsync("taxrecords", vehicleId, body);
        }

        [McpServerTool(Destructive = false, Idempotent = false, OpenWorld = false, Title = "Add note"), Description("Adds a note to a vehicle.")]
        public async Task<string> AddNote(
            [Description("id of the vehicle")] int vehicleId,
            [Description("Short title of the note")] string description,
            [Description("Body of the note")] string noteText,
            [Description("Pin the note to the top of the list")] bool pinned = false,
            [Description("Any extra fields configured for noterecord")] List<ExtraField>? extraFields = null,
            [Description("Optional space-separated tags")] string? tags = null)
        {
            var body = new JsonObject
            {
                ["description"] = description,
                ["noteText"] = noteText,
                ["pinned"] = pinned,
                ["tags"] = tags ?? string.Empty,
                ["extraFields"] = LubeLoggerApiClient.ToJsonArray(extraFields)
            };
            return await AddJsonRecordAsync("notes", vehicleId, body);
        }

        [McpServerTool(Destructive = false, Idempotent = false, OpenWorld = false, Title = "Add plan record"), Description("Adds a planner record: planned service, repair or upgrade work. Progress cannot be set to Done through the API.")]
        public async Task<string> AddPlanRecord(
            [Description("id of the vehicle")] int vehicleId,
            [Description("Description of the planned work")] string description,
            [Description("Estimated cost")] decimal cost,
            [Description("Record type the plan becomes when done")] PlanType type,
            [Description("Priority")] PlanPriority priority = PlanPriority.Normal,
            [Description("Progress")] PlanProgress progress = PlanProgress.Backlog,
            [Description("Any extra fields configured for planrecord")] List<ExtraField>? extraFields = null,
            [Description("Optional notes")] string? notes = null)
        {
            var body = new JsonObject
            {
                ["description"] = description,
                ["cost"] = cost,
                ["type"] = type.ToString(),
                ["priority"] = priority.ToString(),
                ["progress"] = progress.ToString(),
                ["notes"] = notes ?? string.Empty,
                ["extraFields"] = LubeLoggerApiClient.ToJsonArray(extraFields)
            };
            return await AddJsonRecordAsync("planrecords", vehicleId, body);
        }

        [McpServerTool(Destructive = false, Idempotent = false, OpenWorld = false, Title = "Add reminder"), Description("Adds a one-off reminder. Metric Date needs dueDate, Odometer needs dueOdometer, Both needs both. Recurring reminders can't be created through the LubeLogger API; set those up in the LubeLogger UI.")]
        public async Task<string> AddReminder(
            [Description("id of the vehicle")] int vehicleId,
            [Description("What the reminder is for")] string description,
            [Description("What the reminder is due on: Date, Odometer or Both")] ReminderMetric metric,
            [Description("Due date (required for Date and Both)")] DateTime? dueDate = null,
            [Description("Due odometer reading (required for Odometer and Both)")] int? dueOdometer = null,
            [Description("Optional notes")] string? notes = null,
            [Description("Optional space-separated tags")] string? tags = null)
        {
            var body = new JsonObject
            {
                ["description"] = description,
                ["metric"] = metric.ToString(),
                ["notes"] = notes ?? string.Empty,
                ["tags"] = tags ?? string.Empty
            };
            LubeLoggerApiClient.SetIfProvided(body, "dueDate", dueDate);
            LubeLoggerApiClient.SetIfProvided(body, "dueOdometer", dueOdometer);
            return await AddJsonRecordAsync("reminders", vehicleId, body);
        }

        [McpServerTool(Destructive = false, Idempotent = false, OpenWorld = false, Title = "Add equipment record"), Description("Adds an equipment record (e.g. a set of tires or a trailer) to a vehicle.")]
        public async Task<string> AddEquipmentRecord(
            [Description("id of the vehicle")] int vehicleId,
            [Description("Description of the equipment")] string description,
            [Description("Whether the equipment is currently equipped")] bool isEquipped,
            [Description("Any extra fields configured for equipmentrecord")] List<ExtraField>? extraFields = null,
            [Description("Optional notes")] string? notes = null,
            [Description("Optional space-separated tags")] string? tags = null)
        {
            var body = new JsonObject
            {
                ["description"] = description,
                ["isEquipped"] = isEquipped,
                ["notes"] = notes ?? string.Empty,
                ["tags"] = tags ?? string.Empty,
                ["extraFields"] = LubeLoggerApiClient.ToJsonArray(extraFields)
            };
            return await AddJsonRecordAsync("equipmentrecords", vehicleId, body);
        }

        private async Task<string> AddJsonRecordAsync(string recordPath, int vehicleId, JsonObject body)
        {
            var response = await _api.SendJsonAsync(HttpMethod.Post, $"/api/vehicle/{recordPath}/add?vehicleId={vehicleId}", body);
            return response.ToToolResult();
        }
    }
}
