using KarttaBackEnd2.Server.Models;

namespace KarttaBackEnd2.Server.Interfaces
{
    public interface IKMZService
    {
        Task<byte[]> GenerateKmzAsync(FlyToWaylineRequest request);
    }

}
