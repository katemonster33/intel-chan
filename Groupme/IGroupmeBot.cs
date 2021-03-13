using System.Net.Http;
using System.Threading.Tasks;

namespace Groupme
{
    public interface IGroupmeBot
    {
        Task<HttpResponseMessage> Post(string message);
        Task<HttpResponseMessage> Post(string message, string remoteImageUrl);
    }
}