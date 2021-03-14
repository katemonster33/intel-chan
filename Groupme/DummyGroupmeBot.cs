using System.Net.Http;
using System.Threading.Tasks;

namespace Groupme{
    public class DummyGroupmeBot : IGroupmeBot
    {
        public Task Post(string message)
        {
            Console.WriteLine("message");
        }
    }
}