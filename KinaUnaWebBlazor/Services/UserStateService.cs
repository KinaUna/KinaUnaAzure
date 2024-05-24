using KinaUna.Data;
using KinaUna.Data.Models;

namespace KinaUnaWebBlazor.Services
{
    public class UserStateService(IUserInfosHttpClient userInfosHttpClient, IProgenyHttpClient progenyHttpClient, ILocaleManager localeManager)
    {
        private UserInfo? _currentUser;
        private Progeny? _currentProgeny;
        private int? _currentLanguageId;

        public int CurrentLanguageId
        {
            get
            {
                if(_currentLanguageId != null && _currentLanguageId.Value > 0 )
                {
                    return _currentLanguageId.Value;
                }
                return 1;
            }
            set
            {
                if (value > 0)
                {
                    _currentLanguageId = value;
                }
            }
        }
        public UserInfo? CurrentUser { 
            get => _currentUser;
            private set
            {
                if (value != null && value.Id != _currentUser?.Id)
                {
                    _currentUser = value;
                    this.CurrentUserChanged?.Invoke(this, _currentUser);
                }
            } }

        public Progeny? CurrentProgeny
        {
            get => _currentProgeny;
            private set
            {
                if (value != null && value.Id != _currentProgeny?.Id)
                {
                    _currentProgeny = value;
                    this.CurrentProgenyChanged?.Invoke(this, _currentProgeny);
                }
            }
        }

        public async Task SetUser(string userEmail)
        {
            CurrentUser = await userInfosHttpClient.GetUserInfo(userEmail);
            if (CurrentUser != null) await SetProgeny(CurrentUser.ViewChild);
        }

        private async Task SetProgeny(int progenyId)
        {
            if (CurrentUser != null)
            {
                if (progenyId > 0)
                {
                    CurrentProgeny = await progenyHttpClient.GetProgeny(progenyId);
                }
                else
                {
                    CurrentProgeny = await progenyHttpClient.GetProgeny(Constants.DefaultChildId);
                }
                
            }
        }

        public async Task<string> GetTranslation(string word, string page)
        {
            string resultString = await localeManager.GetTranslation(word, page, CurrentLanguageId);
            return resultString;
        }

        public event EventHandler<UserInfo>? CurrentUserChanged;
        public event EventHandler<Progeny>? CurrentProgenyChanged;
    }
}
