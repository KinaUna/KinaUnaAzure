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

namespace KinaUnaWeb.Controllers
{
    public class MeasurementsController : Controller
    {
        private readonly WebDbContext _context;
        private readonly IProgenyHttpClient _progenyHttpClient;
        private int _progId = 2;
        private bool _userIsProgenyAdmin;
        private readonly string _defaultUser = "testuser@niviaq.com";

        public MeasurementsController(WebDbContext context, IProgenyHttpClient progenyHttpClient)
        {
            _context = context;
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
                _progId = 2;
            }

            Progeny progeny = await _progenyHttpClient.GetProgeny(_progId);
            List<UserAccess> accessList = await _progenyHttpClient.GetProgenyAccessList(_progId);

            int userAccessLevel = 5;

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
                userAccessLevel = 0;
            }
            MeasurementViewModel model = new MeasurementViewModel();
            
            List<Measurement> mList = _context.MeasurementsDb.Where(m => m.ProgenyId == _progId).ToList();
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