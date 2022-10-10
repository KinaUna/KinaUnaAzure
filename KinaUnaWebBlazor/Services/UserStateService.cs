using KinaUna.Data.Models;

namespace KinaUnaWebBlazor.Services
{
    public class UserStateService
    {
        private readonly IUserInfosHttpClient _userInfosHttpClient;
        private readonly IProgenyHttpClient _progenyHttpClient;
        private readonly ILocaleManager _localeManager;
        private UserInfo? _currentUser;
        private Progeny? _currentProgeny;
        private int? _currentLanguageId;

        public UserStateService(IUserInfosHttpClient userInfosHttpClient, IProgenyHttpClient progenyHttpClient, ILocaleManager localeManager)
        {
            _userInfosHttpClient = userInfosHttpClient;
            _progenyHttpClient = progenyHttpClient;
            _localeManager = localeManager;
        }

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
            set
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
            set
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
            CurrentUser = await _userInfosHttpClient.GetUserInfo(userEmail);
            await SetProgeny(CurrentUser.ViewChild);
        }

        public async Task SetProgeny(int progenyId)
        {
            if (CurrentUser != null)
            {
                CurrentProgeny = await _progenyHttpClient.GetProgeny(progenyId);
            }
        }

        public async Task<string> GetTranslation(string word, string page)
        {
            string resultString = await _localeManager.GetTranslation(word, page, CurrentLanguageId);
            return resultString;
        }

        public event EventHandler<UserInfo>? CurrentUserChanged;
        public event EventHandler<Progeny>? CurrentProgenyChanged;
    }
}
