using com.itac.mes.imsapi.client.dotnet;
using com.itac.mes.imsapi.domain.container;
using static com.itac.mes.imsapi.client.dotnet.IMSApiDotNetConstants;
using com.itac.artes;
using NLog;
using System.Net.Http.Headers;
using System;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace ValeoItacCheck
{
    public class IMSApi : IIMSApi
    {
        #region Private variables

        private IIMSApiDotNet imsapi = null;

        private IMSApiSessionContextStruct sessionContext = null;

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        #endregion

        #region Public Methods

        public bool ItacConnection()
        {

#if DEBUG
            return true;
#endif
            IMSApiDotNetBase.setProperty(ArtesPropertyNames.PROP_ARTES_APPID, "IMSApiDotNetTestClient");
            IMSApiDotNetBase.setProperty(ArtesPropertyNames.PROP_ARTES_CLUSTERNODES, $"http://{AppSettings.iTAC_Server}:8080/mes/");
            Log.Info($"iTAC server at : {AppSettings.iTAC_Server}");

            imsapi = IMSApiDotNet.loadLibrary();
            IMSApiGetLibraryVersion();

            return IMSApiInit() && RegLogin(AppSettings.iTAC_Station);
        }

        public int ItacShutDown()
        {
            try
            {
#if DEBUG
                int result = 0;
#else
                int result = imsapi.imsapiShutdown();
#endif
                return result;
            }
            catch (Exception ex)
            {
                Log.Error($"An error occurred during iTAC shutdown: {ex.Message} - Stack trace {ex.StackTrace}");

                return -1;
            }
        }

        public bool CheckUser(string station, string username, string password)
        {
            IMSApiSessionValidationStruct sessValData = new IMSApiSessionValidationStruct()
            {
                stationNumber = station,
                stationPassword = "",
                user = username,
                password = password,
                client = "01",
                registrationType = "U",
                systemIdentifier = "01"
            };

            Log.Info($"Trying to connect to iTAC server with following credentials : user name {username} , pass word {password} ");

            int result = imsapi.regLogin(sessValData, out IMSApiSessionContextStruct newSessionContext);
            sessionContext = newSessionContext;

            if (result != RES_OK)
            {
                Log.Error($"CheckUser, error : {result}");
                return false;
            }

            Log.Info("result value: <{result}>", result);
            Log.Info("new session established.");
            Log.Info("===== SessionId: <{sessionId}>", sessionContext.sessionId);
            Log.Info("===== locale: <{locale}>", sessionContext.locale);

            return true;
        }

        public int GetUserLevel(string station, string username)
        {
            string[] attributeCodeArray = { "razeLevel" };
            string[] attributeResultKeys = { "ATTRIBUTE_CODE", "ATTRIBUTE_VALUE", "ERROR_CODE" };

            return imsapi.attribGetAttributeValues(sessionContext, station, 16, username, "-1", attributeCodeArray, 0, attributeResultKeys, out string[] results) != RES_OK
                ? -1
                : int.Parse(results[1]);
        }

        public async Task<(bool Success, string SnState, int ErrorCode)> CheckSerialNumberStateAsync(string station, string snr)
        {

#if DEBUG
            Random random = new Random();
            return (true, random.Next(0, 10).ToString(), 0);
#endif
            Log.Info("Entered CheckSerialNumberStateAsync");
            string[] resultKeys = new string[] { "SERIAL_NUMBER_STATE", "ERROR_CODE" };
            string snState = string.Empty;
            int errorCode = -1;

            bool success = await Task.Run(() =>
            {
                int result = imsapi.trCheckSerialNumberState(sessionContext, station, 2, 0, snr, "-1", resultKeys, out string[] outResults);

                if (outResults.Length >= 2)
                {
                    snState = outResults[0];
                    int.TryParse(outResults[1], out errorCode);
                }

                return result == RES_OK;
            });
            Log.Info($"CheckSerialNumberStateAsync {snr} snState {snState} errorCode {errorCode}");
            return (success, snState, errorCode);
        }

        public async Task<(bool, string[], int)> GetSerialNumberInfoAsync(string station, string snr, string[] inKeys)
        {
            return await Task.Run(() =>
            {
                var result = imsapi.trGetSerialNumberInfo(sessionContext, station, snr, "-1", inKeys, out string[] outResults);
                Log.Info($"GetSerialNumberInfoAsync SNR {snr} result {result}");
                return (result == RES_OK, outResults, result);
            });
        }

        public async Task<List<PanelPositions>> GetPanelSNStateAsync(string station, string snr)
        {
            List<PanelPositions> panelRslt = new List<PanelPositions>();

#if DEBUG
            var fictiveNumberOfBoards = new Random().Next(8, 24);
            Random random = new Random();
            for (int i = 0; i < fictiveNumberOfBoards; i++)
            {
                panelRslt.Add(new PanelPositions
                {
                    PositionNumber = i + 1,
                    SerialNumber = "TG01_9999999999_9999",
                    Status = random.Next(0, 7),
                });
            }
            return panelRslt;
#endif
            int processLayer = 2;
            int checkMultiBoard = 1;
            string serialNumberPos = "-1";
            string[] SnStateResultKeys = new[] { "SERIAL_NUMBER_POS", "SERIAL_NUMBER", "SERIAL_NUMBER_STATE" };
            string[] SnStateResultValues;

            await Task.Run(() =>
            {
                int result = imsapi.trCheckSerialNumberState(sessionContext, station, processLayer, checkMultiBoard, snr, serialNumberPos,
                                                                         SnStateResultKeys, out SnStateResultValues);

                if (!string.IsNullOrEmpty(SnStateResultValues[0]) && SnStateResultValues.Length > 0)
                {
                    for (int i = 0; i < SnStateResultValues.Length; i += SnStateResultKeys.Length)
                    {
                        panelRslt.Add(new PanelPositions
                        {
                            PositionNumber = int.Parse(SnStateResultValues[i]),
                            SerialNumber = SnStateResultValues[i + 1],
                            Status = int.Parse(SnStateResultValues[i + 2]),
                        });
                        Log.Info($"POSITION {SnStateResultValues[i]} SN {SnStateResultValues[i + 1]} STATUS {SnStateResultValues[i + 2]} ");
                    }
                }
            });


            return panelRslt;
        }

        public async Task<string> GetErrorTextAsync(int result)
        {
            return await Task.Run(() =>
            {
                imsapi.imsapiGetErrorText(sessionContext, result, out string errorText);
                return errorText;
            });
        }


        public string[] GetGroups()
        {
            imsapi.imsapiGetGroups(sessionContext, out ImsApiGroupStruct[] groups);

            return groups.Select(a => $"{a.groupName} - {a.groupDescr}").ToArray();
        }

        #endregion

        #region Private Methods

        private void IMSApiGetLibraryVersion()
        {
            _ = imsapi.imsapiGetLibraryVersion(out string version);

            AppSettings.iTAC_VERSION = version;
            Log.Info(version);
        }

        private bool IMSApiInit()
        {
            int result = imsapi.imsapiInit();

            if (result != RES_OK)
            {
                string message;

                // Using a traditional switch statement
                switch (result)
                {
                    case RES_ERR_IMSAPI_ALREADY_INITIALIZED:
                        message = "IMSApi already initialized";
                        break;
                    case RES_ERR_IMSAPI_LOCATOR_INIT_FAILED:
                        message = "IMSApi locator init failed";
                        break;
                    case RES_ERR_IMSAPI_SERVICE_LOOKUP:
                        message = "IMSApi service lookup";
                        break;
                    default:
                        message = "UNKNOWN";
                        break;
                }

                Log.Error("Error Code {result} : {message}", result, message);

                return false;
            }

            return true;
        }


        private bool RegLogin(string station)
        {
            IMSApiSessionValidationStruct sessValData = new IMSApiSessionValidationStruct()
            {
                stationNumber = station,
                stationPassword = "",
                user = "",
                password = "",
                client = "01",
                registrationType = "S",
                systemIdentifier = "01"
            };

            int result = imsapi.regLogin(sessValData, out IMSApiSessionContextStruct newSessionContext);
            sessionContext = newSessionContext;

            if (result != RES_OK)
            {
                Log.Error($"RegisterLogin Error : {result}");
                return false;
            }

            Log.Info("result value: <{result}>", result);
            Log.Info("new session established.");
            Log.Info("===== SessionId: <{sessionId}>", sessionContext.sessionId);
            Log.Info("===== locale: <{locale}>", sessionContext.locale);

            return true;
        }

        public async Task<bool> CheckItacConnectionAsync()
        {
#if DEBUG
            return true;
#endif
            try
            {
                using (Ping ping = new Ping())
                {
                    PingReply reply = await ping.SendPingAsync(AppSettings.iTAC_Server, 1000);

                    return reply.Status == IPStatus.Success;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        #endregion
    }
}
