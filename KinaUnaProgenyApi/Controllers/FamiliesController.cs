using KinaUna.Data;
using KinaUna.Data.Extensions;
using KinaUna.Data.Models;
using KinaUna.Data.Models.Family;
using KinaUnaProgenyApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using KinaUnaProgenyApi.Services.FamiliesServices;

namespace KinaUnaProgenyApi.Controllers
{
    [Authorize(Policy = "UserOrClient")]
    [Produces("application/json")]
    [Route("api/[controller]")]
    [ApiController]
    public class FamiliesController(IFamiliesService familyService, IFamilyMembersService familyMembersService, IUserInfoService userInfoService) : ControllerBase
    {
        [HttpGet]
        [Route("[action]/{familyId:int}")]
        public async Task<IActionResult> GetFamily(int familyId)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserInfo currenUserInfo = await userInfoService.GetUserInfoByEmail(userEmail);

            Family family = await familyService.GetFamilyById(familyId, currenUserInfo);
            
            if (family.FamilyId == 0)
            {
                return Unauthorized();
            }

            return Ok(family);
        }

        [HttpGet]
        [Route("[action]")]
        public async Task<IActionResult> GetCurrentUsersFamilies()
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserInfo currenUserInfo = await userInfoService.GetUserInfoByEmail(userEmail);

            List<Family> families = await familyService.GetUsersFamiliesByUserId(currenUserInfo.UserId, currenUserInfo);
            
            return Ok(families);
        }

        [HttpPost]
        public async Task<IActionResult> AddFamily([FromBody] Family family)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserInfo currenUserInfo = await userInfoService.GetUserInfoByEmail(userEmail);

            Family newFamily = await familyService.AddFamily(family, currenUserInfo);

            return Ok(newFamily);
        }

        [HttpPut("{familyId:int}")]
        public async Task<IActionResult> UpdateFamily(int familyId, [FromBody] Family family)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserInfo currenUserInfo = await userInfoService.GetUserInfoByEmail(userEmail);

            Family existingFamily = await familyService.GetFamilyById(familyId, currenUserInfo);
            if (existingFamily.FamilyId == 0)
            {
                return Unauthorized();
            }

            if (family.FamilyId != familyId)
            {
                return BadRequest("Family ID in URL does not match Family ID in body.");
            }

            Family updatedFamily = await familyService.UpdateFamily(family, currenUserInfo);
            
            if (updatedFamily.FamilyId == 0)
            {
                return Unauthorized();
            }

            return Ok(updatedFamily);
        }

        [HttpDelete("{familyId:int}")]
        public async Task<IActionResult> DeleteFamily(int familyId)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserInfo currenUserInfo = await userInfoService.GetUserInfoByEmail(userEmail);
            
            Family existingFamily = await familyService.GetFamilyById(familyId, currenUserInfo);
            if (existingFamily.FamilyId == 0)
            {
                return Unauthorized();
            }
            
            bool result = await familyService.DeleteFamily(existingFamily.FamilyId, currenUserInfo);
            if (!result)
            {
                return Unauthorized();
            }

            return Ok(true);
        }

        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> GetFamilyMembers([FromBody]Family family)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserInfo currenUserInfo = await userInfoService.GetUserInfoByEmail(userEmail);

            List<FamilyMember> familyMembers = await familyMembersService.GetFamilyMembersForFamily(family.FamilyId, currenUserInfo);

            return Ok(familyMembers);
        }
    }
}
