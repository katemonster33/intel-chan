using System.Net.Http;
using System.Threading.Tasks;

namespace Groupme
{
    public interface IGroupmeBot
    {
        Task Post(string message);
        
    }
}