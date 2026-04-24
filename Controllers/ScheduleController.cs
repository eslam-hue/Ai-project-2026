using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ai_csp.Services;

namespace ai_csp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ScheduleController : ControllerBase
    {
        private readonly SchedulerService _scheduler;

        public ScheduleController()
        {
            _scheduler = new SchedulerService();
        }

        [HttpGet]
        public IActionResult Get()
        {
            var result = _scheduler.GenerateSchedule();
            return Ok(result);
        }
    }
}
