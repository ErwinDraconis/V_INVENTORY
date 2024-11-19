using System.Collections.Generic;
using System.Threading.Tasks;

namespace ValeoItacCheck
{
    public interface IIMSApi
    {
        bool ItacConnection();

        int ItacShutDown();

        bool CheckUser(string station, string username, string password);

        int GetUserLevel(string station, string username);

        Task<(bool Success, string SnState, int ErrorCode)> CheckSerialNumberStateAsync(string station, string snr);

        Task<(bool, string[], int)> GetSerialNumberInfoAsync(string station, string snr, string[] inKeys);

        Task<List<PanelPositions>> GetPanelSNStateAsync(string station, string snr);

        Task<string> GetErrorTextAsync(int result);

        Task<bool> CheckItacConnectionAsync();
    }
}
