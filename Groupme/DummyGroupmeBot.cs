using System.Net.Http;
using System.Threading.Tasks;

namespace Groupme{
    public class DummyGroupmeBot : IGroupmeBot
    {
        public Task<HttpResponseMessage> Post(string message)
        {
            throw new System.NotImplementedException();
        }

        public Task<HttpResponseMessage> Post(string message, string remoteImageUrl)
        {
            throw new System.NotImplementedException();
        }
    }
}