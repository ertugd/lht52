using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace lht52
{
    [Authorize]
    public class TelemetryHub : Hub
    {
    }
}
