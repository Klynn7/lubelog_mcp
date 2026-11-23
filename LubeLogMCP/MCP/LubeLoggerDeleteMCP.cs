using LubeLogMCP.Helper;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace LubeLogMCP.MCP
{
    /// <summary>
    /// Delete tools. Deliberately NOT marked [McpServerToolType], so WithToolsFromAssembly() skips them;
    /// Program.cs registers this class only when LUBELOG_ENABLE_DELETE=true. LubeLogger also requires a Manager key
    /// (Delete permission).
    /// </summary>
    public class LubeLoggerDeleteMCP
    {
        private const string DeleteWarning = " This permanently deletes the record and cannot be undone; confirm with the user first.";
        private readonly LubeLoggerApiClient _api;
        public LubeLoggerDeleteMCP(LubeLoggerApiClient api)
        {
            _api = api;
        }

        [McpServerTool(Destructive = true), Description("Deletes a service record." + DeleteWarning)]
        public Task<string> DeleteServiceRecord([Description("id of the service record")] int id)
            => _api.DeleteRecordAsync("servicerecords", id);

        [McpServerTool(Destructive = true), Description("Deletes a repair record." + DeleteWarning)]
        public Task<string> DeleteRepairRecord([Description("id of the repair record")] int id)
            => _api.DeleteRecordAsync("repairrecords", id);

        [McpServerTool(Destructive = true), Description("Deletes an upgrade record." + DeleteWarning)]
        public Task<string> DeleteUpgradeRecord([Description("id of the upgrade record")] int id)
            => _api.DeleteRecordAsync("upgraderecords", id);

        [McpServerTool(Destructive = true), Description("Deletes a fuel (gas) record." + DeleteWarning)]
        public Task<string> DeleteFuelRecord([Description("id of the fuel record")] int id)
            => _api.DeleteRecordAsync("gasrecords", id);

        [McpServerTool(Destructive = true), Description("Deletes an odometer record." + DeleteWarning)]
        public Task<string> DeleteOdometerRecord([Description("id of the odometer record")] int id)
            => _api.DeleteRecordAsync("odometerrecords", id);

        [McpServerTool(Destructive = true), Description("Deletes a supply record (vehicle or shop supply)." + DeleteWarning)]
        public Task<string> DeleteSupplyRecord([Description("id of the supply record")] int id)
            => _api.DeleteRecordAsync("supplyrecords", id);

        [McpServerTool(Destructive = true), Description("Deletes a tax record." + DeleteWarning)]
        public Task<string> DeleteTaxRecord([Description("id of the tax record")] int id)
            => _api.DeleteRecordAsync("taxrecords", id);

        [McpServerTool(Destructive = true), Description("Deletes a note." + DeleteWarning)]
        public Task<string> DeleteNote([Description("id of the note")] int id)
            => _api.DeleteRecordAsync("notes", id);

        [McpServerTool(Destructive = true), Description("Deletes a planner record." + DeleteWarning)]
        public Task<string> DeletePlanRecord([Description("id of the plan")] int id)
            => _api.DeleteRecordAsync("planrecords", id);

        [McpServerTool(Destructive = true), Description("Deletes a reminder." + DeleteWarning)]
        public Task<string> DeleteReminder([Description("id of the reminder")] int id)
            => _api.DeleteRecordAsync("reminders", id);

        [McpServerTool(Destructive = true), Description("Deletes an equipment record." + DeleteWarning)]
        public Task<string> DeleteEquipmentRecord([Description("id of the equipment record")] int id)
            => _api.DeleteRecordAsync("equipmentrecords", id);
    }
}
