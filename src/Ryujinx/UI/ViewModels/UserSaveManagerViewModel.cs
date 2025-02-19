using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData;
using DynamicData.Binding;
using Ryujinx.Ava.Common.Locale;
using Ryujinx.Ava.UI.Models;
using Ryujinx.HLE.HOS.Services.Account.Acc;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Ryujinx.Ava.UI.ViewModels
{
    public partial class UserSaveManagerViewModel : BaseModel
    {
        [ObservableProperty] private int _sortIndex;
        [ObservableProperty] private int _orderIndex;
        [ObservableProperty] private string _search;
        [ObservableProperty] private ObservableCollection<SaveModel> _saves = [];
        [ObservableProperty] private ObservableCollection<SaveModel> _views = [];
        private readonly AccountManager _accountManager;

        public string SaveManagerHeading => LocaleManager.Instance.UpdateAndGetDynamicValue(LocaleKeys.SaveManagerHeading, _accountManager.LastOpenedUser.Name, _accountManager.LastOpenedUser.UserId);

        public UserSaveManagerViewModel(AccountManager accountManager)
        {
            _accountManager = accountManager;
            PropertyChanged += (_, evt) =>
            {
                if (evt.PropertyName is
                    nameof(SortIndex) or
                    nameof(OrderIndex) or
                    nameof(Search) or
                    nameof(Saves))
                {
                    Sort();
                }
            };
        }

        public void Sort()
        {
            Saves.AsObservableChangeSet()
                .Filter(Filter)
                .Sort(GetComparer())
                .Bind(out ReadOnlyObservableCollection<SaveModel> view).AsObservableList();

#pragma warning disable MVVMTK0034
            _views.Clear();
            _views.AddRange(view);
#pragma warning restore MVVMTK0034
            OnPropertyChanged(nameof(Views));
        }

        private bool Filter(object arg)
        {
            if (arg is SaveModel save)
            {
                return string.IsNullOrWhiteSpace(Search) || save.Title.ToLower().Contains(Search.ToLower());
            }

            return false;
        }

        private IComparer<SaveModel> GetComparer()
        {
            return SortIndex switch
            {
                0 => OrderIndex == 0
                    ? SortExpressionComparer<SaveModel>.Ascending(save => save.Title)
                    : SortExpressionComparer<SaveModel>.Descending(save => save.Title),
                1 => OrderIndex == 0
                    ? SortExpressionComparer<SaveModel>.Ascending(save => save.Size)
                    : SortExpressionComparer<SaveModel>.Descending(save => save.Size),
                _ => null,
            };
        }
    }
}
