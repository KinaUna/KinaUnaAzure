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
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByEmail(userEmail);

            Family family = await familyService.GetFamilyById(familyId, currentUserInfo);
            
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
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByEmail(userEmail);

            List<Family> families = await familyService.GetUsersFamiliesByUserId(currentUserInfo.UserId, currentUserInfo);
            
            return Ok(families);
        }

        [HttpPost]
        public async Task<IActionResult> AddFamily([FromBody] Family family)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByEmail(userEmail);

            Family newFamily = await familyService.AddFamily(family, currentUserInfo);

            return Ok(newFamily);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateFamily([FromBody] Family family)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByEmail(userEmail);
            
            Family updatedFamily = await familyService.UpdateFamily(family, currentUserInfo);
            
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
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByEmail(userEmail);
            
            Family existingFamily = await familyService.GetFamilyById(familyId, currentUserInfo);
            if (existingFamily.FamilyId == 0)
            {
                return Unauthorized();
            }
            
            bool result = await familyService.DeleteFamily(existingFamily.FamilyId, currentUserInfo);
            if (!result)
            {
                return Unauthorized();
            }

            return Ok(true);
        }

        [HttpGet]
        [Route("[action]/{familyId:int}")]
        public async Task<IActionResult> GetFamilyMembersForFamily(int familyId)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByEmail(userEmail);
            List<FamilyMember> familyMembers = await familyMembersService.GetFamilyMembersForFamily(familyId, currentUserInfo);
            return Ok(familyMembers);
        }

        [HttpGet]
        [Route("[action]/{familyMemberId:int}")]
        public async Task<IActionResult> GetFamilyMember(int familyMemberId)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByEmail(userEmail);
            FamilyMember familyMember = await familyMembersService.GetFamilyMember(familyMemberId, currentUserInfo);
            
            if (familyMember.FamilyMemberId == 0)
            {
                return Unauthorized();
            }
            
            return Ok(familyMember);
        }


        [HttpPost]
        [Route("[action]")]
        public async Task<IActionResult> AddFamilyMember([FromBody] FamilyMember familyMember)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByEmail(userEmail);
            FamilyMember newFamilyMember = await familyMembersService.AddFamilyMember(familyMember, familyMember.PermissionLevel, currentUserInfo);
            
            if (newFamilyMember.FamilyMemberId == 0)
            {
                return Unauthorized();
            }
            
            return Ok(newFamilyMember);
        }
        
        [HttpPut]
        [Route("[action]")]
        public async Task<IActionResult> UpdateFamilyMember([FromBody] FamilyMember familyMember)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByEmail(userEmail);
            
            FamilyMember updatedFamilyMember = await familyMembersService.UpdateFamilyMember(familyMember, currentUserInfo);
            
            if (updatedFamilyMember.FamilyMemberId == 0)
            {
                return Unauthorized();
            }

            return Ok(updatedFamilyMember);
        }

        [HttpDelete]
        [Route("[action]/{familyMemberId:int}")]
        public async Task<IActionResult> DeleteFamilyMember(int familyMemberId)
        {
            string userEmail = User.GetEmail() ?? Constants.DefaultUserEmail;
            UserInfo currentUserInfo = await userInfoService.GetUserInfoByEmail(userEmail);
            
            bool result = await familyMembersService.DeleteFamilyMember(familyMemberId, currentUserInfo);
            if (!result)
            {
                return Unauthorized();
            }
            return Ok(true);
        }

    }
}
