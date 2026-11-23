using LubeLogMCP.Helper;
using LubeLogMCP.Models;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text;
using System.Text.Json;

namespace LubeLogMCP.MCP
{
    [McpServerToolType]
    public partial class LubeLoggerMCP
    {
        private readonly LubeLoggerApiClient _api;
        public LubeLoggerMCP(LubeLoggerApiClient api)
        {
            _api = api;
        }
        [McpServerTool, Description("Gets status of LubeLogger MCP.")]
        public async Task<string> GetLubeLoggerMCPStatus()
        {
            string result = $"MCP Version: {StaticHelper.VersionNumber}";
            result += Environment.NewLine;
            if (!string.IsNullOrWhiteSpace(_api.Instance))
            {
                result += $"MCP Server Configured for {_api.Instance}";
                result += Environment.NewLine;
                result += $"Auth: {_api.DescribeAuthSource()}";
                result += Environment.NewLine;
                var response = await _api.GetAsync("/api/version");
                if (!response.Success)
                {
                    result += $"Failed to connect to LubeLogger instance: {response.ToToolResult()}";
                }
                else
                {
                    try
                    {
                        var serverResponse = JsonSerializer.Deserialize<ServerVersion>(response.Body, JsonOptions);
                        if (!string.IsNullOrWhiteSpace(serverResponse?.CurrentVersion))
                        {
                            result += $"LubeLogger Version: {serverResponse.CurrentVersion}";
                        }
                    }
                    catch (JsonException ex)
                    {
                        result += $"Failed to read LubeLogger version: {ex.Message}";
                    }
                }
            }
            else
            {
                result += "LubeLogger Instance not Configured";
            }
            return result;
        }
        [McpServerTool, Description("Gets vehicles in garage.")]
        public async Task<string> GetVehicles()
        {
            var response = await _api.GetAsync("/api/vehicles");
            if (!response.Success)
            {
                return response.ToToolResult();
            }
            try
            {
                var result = JsonSerializer.Deserialize<List<Vehicle>>(response.Body, JsonOptions);
                var resultString = string.Empty;
                foreach (Vehicle vehicle in result ?? new List<Vehicle>())
                {
                    resultString += $"Id: {vehicle.Id} - {vehicle.Year} {vehicle.Make} {vehicle.Model}({vehicle.Identifier})";
                    resultString += Environment.NewLine;
                }
                return resultString;
            }
            catch (JsonException ex)
            {
                return $"Error: unexpected response from LubeLogger: {ex.Message}";
            }
        }
        [McpServerTool, Description("Check if a vehicle is an electric vehicle")]
        public async Task<string> GetVehicleIsElectric([Description("id of the vehicle")] int vehicleId)
        {
            var response = await _api.GetAsync("/api/vehicles");
            if (!response.Success)
            {
                return response.ToToolResult();
            }
            try
            {
                var result = JsonSerializer.Deserialize<List<Vehicle>>(response.Body, JsonOptions);
                foreach (Vehicle vehicle in result ?? new List<Vehicle>())
                {
                    if (vehicle.Id == vehicleId && vehicle.IsElectric)
                    {
                        return "this is an electric vehicle";
                    }
                }
                return "this is not an electric vehicle";
            }
            catch (JsonException ex)
            {
                return $"Error: unexpected response from LubeLogger: {ex.Message}";
            }
        }
        [McpServerTool, Description("Adds a fuel record.")]
        public async Task<string> AddFuelRecord(
            [Description("id of the vehicle")] int vehicleId,
            [Description("Date of fuel up")] DateTime date,
            [Description("Odometer at time of fuel up")] int odometer,
            [Description("Volume of gas pumped")] decimal volume,
            [Description("Total cost of fuel up")] decimal cost,
            [Description("Is fueled up completely")] bool fillToFull,
            [Description("Any missed fuel ups")] bool missedFuelUp,
            [Description("State of Charge at beginning of charge session if charging an electric vehicle, default to 20 if not an electric vehicle")] int startingSoc,
            [Description("State of Charge at end of charge session if charging an electric vehicle, default to 80 if not an electric vehicle")] int endingSoc,
            [Description("Any extra fields configured for gasrecord")] List<ExtraField> extraFields,
            [Description("Optional notes")] string? notes = null,
            [Description("Optional space-separated tags")] string? tags = null)
        {
            var requestData = new PostRequestModel
            {
                Date = date.ToString("yyyy-MM-dd"),
                Odometer = odometer,
                FuelConsumed = volume,
                Cost = cost,
                IsFillToFull = fillToFull,
                MissedFuelUp = missedFuelUp,
                StartingSoc = startingSoc,
                EndingSoc = endingSoc
            };
            return await PostRecordAsync("gasrecords", vehicleId, requestData, extraFields, notes, tags);
        }
        [McpServerTool, Description("Adds a service record.")]
        public async Task<string> AddServiceRecord(
            [Description("id of the vehicle")] int vehicleId,
            [Description("Date serviced")] DateTime date,
            [Description("Odometer at time of service")] int odometer,
            [Description("Description of items serviced")] string description,
            [Description("Total cost of the service")] decimal cost,
            [Description("Any extra fields configured for servicerecord")] List<ExtraField> extraFields,
            [Description("Optional notes")] string? notes = null,
            [Description("Optional space-separated tags")] string? tags = null)
        {
            var requestData = new PostRequestModel
            {
                Date = date.ToString("yyyy-MM-dd"),
                Odometer = odometer,
                Description = description,
                Cost = cost
            };
            return await PostRecordAsync("servicerecords", vehicleId, requestData, extraFields, notes, tags);
        }
        [McpServerTool, Description("Adds a repair record.")]
        public async Task<string> AddRepairRecord(
            [Description("id of the vehicle")] int vehicleId,
            [Description("Date repaired")] DateTime date,
            [Description("Odometer at time of repair")] int odometer,
            [Description("Description of items repaired")] string description,
            [Description("Total cost of the repair")] decimal cost,
            [Description("Any extra fields configured for repairrecord")] List<ExtraField> extraFields,
            [Description("Optional notes")] string? notes = null,
            [Description("Optional space-separated tags")] string? tags = null)
        {
            var requestData = new PostRequestModel
            {
                Date = date.ToString("yyyy-MM-dd"),
                Odometer = odometer,
                Description = description,
                Cost = cost
            };
            return await PostRecordAsync("repairrecords", vehicleId, requestData, extraFields, notes, tags);
        }
        [McpServerTool, Description("Adds an upgrade record.")]
        public async Task<string> AddUpgradeRecord(
            [Description("id of the vehicle")] int vehicleId,
            [Description("Date upgraded")] DateTime date,
            [Description("Odometer at time of upgrade")] int odometer,
            [Description("Description of items upgraded")] string description,
            [Description("Total cost of the upgrade")] decimal cost,
            [Description("Any extra fields configured for upgraderecord")] List<ExtraField> extraFields,
            [Description("Optional notes")] string? notes = null,
            [Description("Optional space-separated tags")] string? tags = null)
        {
            var requestData = new PostRequestModel
            {
                Date = date.ToString("yyyy-MM-dd"),
                Odometer = odometer,
                Description = description,
                Cost = cost
            };
            return await PostRecordAsync("upgraderecords", vehicleId, requestData, extraFields, notes, tags);
        }
        [McpServerTool, Description("Adds an odometer record.")]
        public async Task<string> AddOdometerRecord(
            [Description("id of the vehicle")] int vehicleId,
            [Description("Date recorded")] DateTime date,
            [Description("Odometer recorded")] int odometer,
            [Description("Any extra fields configured for odometerrecord")] List<ExtraField> extraFields,
            [Description("Ids of equipment equipped for the vehicle")] List<int> equipmentRecordIds,
            [Description("Optional notes")] string? notes = null,
            [Description("Optional space-separated tags")] string? tags = null)
        {
            var requestData = new PostRequestModel
            {
                Date = date.ToString("yyyy-MM-dd"),
                Odometer = odometer
            };

            if (equipmentRecordIds.Any())
            {
                requestData.EquipmentRecordId = string.Join(' ', equipmentRecordIds);
            }
            return await PostRecordAsync("odometerrecords", vehicleId, requestData, extraFields, notes, tags);
        }
        [McpServerTool, Description("Get Equipped Equipment for a vehicle")]
        public async Task<string> GetEquippedEquipment(
            [Description("id of the vehicle")] int vehicleId
            )
        {
            var response = await _api.GetAsync($"/api/vehicle/equipmentrecords?vehicleId={vehicleId}");
            if (!response.Success)
            {
                return response.ToToolResult();
            }
            try
            {
                var result = JsonSerializer.Deserialize<List<Equipment>>(response.Body, JsonOptions);
                result?.RemoveAll(x => !x.IsEquipped);
                var serializedResult = JsonSerializer.Serialize(result);
                return serializedResult;
            }
            catch (JsonException ex)
            {
                return $"Error: unexpected response from LubeLogger: {ex.Message}";
            }
        }
        [McpServerTool, Description("Adds a supply record.")]
        public async Task<string> AddSupplyRecord(
            [Description("id of the vehicle")] int vehicleId,
            [Description("Date purchased")] DateTime date,
            [Description("Description of the supply")] string description,
            [Description("Quantity purchased")] decimal quantity,
            [Description("Cost of the supply")] decimal cost,
            [Description("Part number")] string partNumber,
            [Description("Part supplier")] string partSupplier,
            [Description("Any extra fields configured for supplyrecord")] List<ExtraField> extraFields,
            [Description("Optional notes")] string? notes = null,
            [Description("Optional space-separated tags")] string? tags = null)
        {
            var requestData = new PostRequestModel
            {
                Date = date.ToString("yyyy-MM-dd"),
                Description = description,
                PartQuantity = quantity,
                Cost = cost,
                PartNumber = partNumber,
                PartSupplier = partSupplier
            };
            return await PostRecordAsync("supplyrecords", vehicleId, requestData, extraFields, notes, tags);
        }
        [McpServerTool, Description("Adds a shop supply record.")]
        public async Task<string> AddShopSupplyRecord(
            [Description("Date purchased")] DateTime date,
            [Description("Description of the supply")] string description,
            [Description("Quantity purchased")] decimal quantity,
            [Description("Cost of the supply")] decimal cost,
            [Description("Part number")] string partNumber,
            [Description("Part supplier")] string partSupplier,
            [Description("Any extra fields configured for supplyrecord")] List<ExtraField> extraFields,
            [Description("Optional notes")] string? notes = null,
            [Description("Optional space-separated tags")] string? tags = null)
        {
            var requestData = new PostRequestModel
            {
                Date = date.ToString("yyyy-MM-dd"),
                Description = description,
                PartQuantity = quantity,
                Cost = cost,
                PartNumber = partNumber,
                PartSupplier = partSupplier
            };
            return await PostRecordAsync("supplyrecords", 0, requestData, extraFields, notes, tags);
        }
        [McpServerTool, Description("Adds a vehicle record.")]
        public async Task<string> AddVehicleRecord(
            [Description("Model year of the vehicle")] int year,
            [Description("Make of the vehicle")] string make,
            [Description("Model of the vehicle")] string model,
            [Description("Optional license plate of the vehicle")] string licensePlate,
            [Description("Vehicle use engine hours")] bool useEngineHours,
            [Description("Odometer is optional for vehicle")] bool odometerOptional,
            [Description("Fuel type for the vehicle")] FuelType fuelType,
            [Description("Any extra fields configured for vehiclerecord")] List<ExtraField> extraFields)
        {
            var dataParams = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("year", year.ToString()),
                new KeyValuePair<string, string>("make", make),
                new KeyValuePair<string, string>("model", model),
                new KeyValuePair<string, string>("useEngineHours", useEngineHours.ToString()),
                new KeyValuePair<string, string>("odometerOptional", odometerOptional.ToString()),
                new KeyValuePair<string, string>("identifier", "LicensePlate"),
                new KeyValuePair<string, string>("licensePlate", licensePlate),
                new KeyValuePair<string, string>("fuelType", fuelType.ToString())
            };

            for (int i = 0; i < extraFields.Count; i++)
            {
                dataParams.Add(new KeyValuePair<string, string>($"extraFields[{i}][name]", extraFields[i].Name));
                dataParams.Add(new KeyValuePair<string, string>($"extraFields[{i}][value]", extraFields[i].Value));
            }

            var response = await _api.SendAsync(HttpMethod.Post, "/api/vehicles/add", new FormUrlEncodedContent(dataParams));
            return response.ToToolResult();
        }
        [McpServerTool, Description("Gets latest odometer reading for a vehicle.")]
        public async Task<string> GetLatestOdometer(
            [Description("id of the vehicle")] int vehicleId
            )
        {
            var response = await _api.GetAsync($"/api/vehicle/odometerrecords/latest?vehicleId={vehicleId}");
            return response.ToToolResult();
        }
        [McpServerTool, Description("Get Extra Fields for a Record Type")]
        public async Task<string> GetExtraFields(
            [Description("Record type")] ImportMode importMode
            )
        {
            var response = await _api.GetAsync("/api/extrafields");
            if (!response.Success)
            {
                return response.ToToolResult();
            }
            try
            {
                var result = JsonSerializer.Deserialize<List<ExtraFieldsVM>>(response.Body, JsonOptions);
                result?.RemoveAll(x => x.RecordType != importMode.ToString());
                var serializedResult = JsonSerializer.Serialize(result);
                return serializedResult;
            }
            catch (JsonException ex)
            {
                return $"Error: unexpected response from LubeLogger: {ex.Message}";
            }
        }

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        /// <summary>
        /// Posts one of upstream's PostRequestModel bodies to /api/vehicle/{recordPath}/add.
        /// </summary>
        private async Task<string> PostRecordAsync(string recordPath, int vehicleId, PostRequestModel requestData, List<ExtraField>? extraFields, string? notes, string? tags)
        {
            foreach (var extraField in extraFields ?? new List<ExtraField>())
            {
                requestData.ExtraFields.Add(new ExtraFieldPostModel { Name = extraField.Name, Value = extraField.Value });
            }
            requestData.Notes = notes ?? string.Empty;
            requestData.Tags = tags ?? string.Empty;
            var response = await _api.SendAsync(
                HttpMethod.Post,
                $"/api/vehicle/{recordPath}/add?vehicleId={vehicleId}",
                LubeLoggerApiClient.JsonContent(JsonSerializer.Serialize(requestData)));
            return response.ToToolResult();
        }
    }
}
