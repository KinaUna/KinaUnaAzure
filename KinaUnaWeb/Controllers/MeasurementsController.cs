using KinaUnaWeb.Data;
using KinaUnaWeb.Models;
using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace KinaUnaWeb.Controllers
{
    public class MeasurementsController : Controller
    {
        private readonly WebDbContext _context;
        private readonly IProgenyHttpClient _progenyHttpClient;
        private int _progId = Constants.DefaultChildId;
        private bool _userIsProgenyAdmin;
        private readonly string _defaultUser = Constants.DefaultUserEmail;

        public MeasurementsController(WebDbContext context, IProgenyHttpClient progenyHttpClient)
        {
            _context = context; // Todo: Replace _context with httpClient
            _progenyHttpClient = progenyHttpClient;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(int childId = 0)
        {
            _progId = childId;
            string userEmail = HttpContext.User.FindFirst("email")?.Value ?? _defaultUser;
            
            UserInfo userinfo = await _progenyHttpClient.GetUserInfo(userEmail);
            if (childId == 0 && userinfo.ViewChild > 0)
            {
                _progId = userinfo.ViewChild;
            }

            if (_progId == 0)
            {
                _progId = Constants.DefaultChildId;
            }

            Progeny progeny = await _progenyHttpClient.GetProgeny(_progId);
            List<UserAccess> accessList = await _progenyHttpClient.GetProgenyAccessList(_progId);

            int userAccessLevel = (int)AccessLevel.Public;

            if (accessList.Count != 0)
            {
                UserAccess userAccess = accessList.SingleOrDefault(u => u.UserId.ToUpper() == userEmail.ToUpper());
                if (userAccess != null)
                {
                    userAccessLevel = userAccess.AccessLevel;
                }
            }

            if (progeny.Admins.ToUpper().Contains(userEmail.ToUpper()))
            {
                _userIsProgenyAdmin = true;
                userAccessLevel = (int)AccessLevel.Private;
            }
            MeasurementViewModel model = new MeasurementViewModel();
            
            // ToDo: Implement _progenyClient.GetMeasurements()
            List<Measurement> mList = _context.MeasurementsDb.AsNoTracking().Where(m => m.ProgenyId == _progId).ToList();
            List<Measurement> measurementsList = new List<Measurement>();
            foreach (Measurement m in mList)
            {
                if (m.AccessLevel >= userAccessLevel)
                {
                    measurementsList.Add(m);
                }
            }
            measurementsList = measurementsList.OrderBy(m => m.Date).ToList();
            if (measurementsList.Count != 0)
            {
                model.MeasurementsList = measurementsList;

            }
            else
            {
                Measurement m = new Measurement();
                m.ProgenyId = _progId;
                m.Date = DateTime.UtcNow;
                m.CreatedDate = DateTime.UtcNow;
                model.MeasurementsList = new List<Measurement>();
                model.MeasurementsList.Add(m);
            }
            model.IsAdmin = _userIsProgenyAdmin;
            model.Progeny = progeny;
            return View(model);
        }
    }
}