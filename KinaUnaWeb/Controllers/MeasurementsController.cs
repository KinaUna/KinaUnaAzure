using KinaUnaWeb.Models.ItemViewModels;
using KinaUnaWeb.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;

namespace KinaUnaWeb.Controllers
{
    public class MeasurementsController : Controller
    {
        private readonly IProgenyHttpClient _progenyHttpClient;
        private readonly IUserInfosHttpClient _userInfosHttpClient;
        private readonly IMeasurementsHttpClient _measurementsHttpClient;
        private readonly IUserAccessHttpClient _userAccessHttpClient;
        
        public MeasurementsController(IProgenyHttpClient progenyHttpClient, IUserInfosHttpClient userInfosHttpClient, IMeasurementsHttpClient measurementsHttpClient, IUserAccessHttpClient userAccessHttpClient)
        {
            _progenyHttpClient = progenyHttpClient;
            _userInfosHttpClient = userInfosHttpClient;
            _measurementsHttpClient = measurementsHttpClient;
            _userAccessHttpClient = userAccessHttpClient;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index(int childId = 0)
        {
            MeasurementViewModel model = new MeasurementViewModel();
            model.LanguageId = Request.GetLanguageIdFromCookie();
            string userEmail = User.GetEmail();
            model.CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);


            if (childId == 0 && model.CurrentUser.ViewChild > 0)
            {
                childId = model.CurrentUser.ViewChild;
            }

            if (childId == 0)
            {
                childId = Constants.DefaultChildId;
            }

            Progeny progeny = await _progenyHttpClient.GetProgeny(childId);
            List<UserAccess> accessList = await _userAccessHttpClient.GetProgenyAccessList(childId);

            int userAccessLevel = (int)AccessLevel.Public;

            if (accessList.Count != 0)
            {
                UserAccess userAccess = accessList.SingleOrDefault(u => u.UserId.ToUpper() == userEmail.ToUpper());
                if (userAccess != null)
                {
                    userAccessLevel = userAccess.AccessLevel;
                }
            }

            if (progeny.IsInAdminList(userEmail))
            {
                model.IsAdmin = true;
                userAccessLevel = (int)AccessLevel.Private;
            }
            
            
            // ToDo: Implement _progenyClient.GetMeasurements()
            List<Measurement> mList = await _measurementsHttpClient.GetMeasurementsList(childId, userAccessLevel);
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
                m.ProgenyId = childId;
                m.Date = DateTime.UtcNow;
                m.CreatedDate = DateTime.UtcNow;
                model.MeasurementsList = new List<Measurement>();
                model.MeasurementsList.Add(m);
            }
            model.Progeny = progeny;
            
            return View(model);
        }
    }
}