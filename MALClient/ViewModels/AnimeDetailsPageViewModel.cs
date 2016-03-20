﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Xml.Linq;
using Windows.System;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using MALClient.Comm;
using MALClient.Items;
using MALClient.Models;
using MALClient.Pages;

namespace MALClient.ViewModels
{
    public class AnimeDetailsPageNavigationArgs
    {
        public readonly XElement AnimeElement;
        public readonly IAnimeData AnimeItem;
        public readonly int Id;
        public readonly object PrevPageSetup;
        public readonly string Title;
        public PageIndex Source;
        public bool RegisterBackNav = true;
        public bool AnimeMode = true;

        public AnimeDetailsPageNavigationArgs(int id, string title, XElement element, IAnimeData animeReference,
            object args = null)
        {
            Id = id;
            Title = title;
            AnimeElement = element;
            PrevPageSetup = args;
            AnimeItem = animeReference;
        }
    }

    public interface IDetailsViewInteraction
    {
        Flyout GetWatchedEpsFlyout();
    }

    public class AnimeDetailsPageViewModel : ViewModelBase
    {
        private IAnimeData _animeItemReference;
        private float _globalScore;
        private string _imgUrl;
        private bool _loadedDetails;
        private bool _loadedReviews;
        private bool _loadedRecomm;
        private bool _loadedRelated;
        public ObservableCollection<ListViewItem> LeftDetailsRow => _loadedItems1; 
        public ObservableCollection<ListViewItem> RightDetailsRow => _loadedItems2; 
        public ObservableCollection<ListViewItem> LeftGenres => _genres1;
        public ObservableCollection<ListViewItem> RightGenres => _genres2; 
        public ObservableCollection<ListViewItem> Episodes => _episodes; 
        public ObservableCollection<ListViewItem> OPs => _ops; 
        public ObservableCollection<ListViewItem> EDs => _eds;
        public ObservableCollection<AnimeReviewData> Reviews { get;} = new ObservableCollection<AnimeReviewData>(); 
        public ObservableCollection<DirectRecommendationData> Recommendations { get;} = new ObservableCollection<DirectRecommendationData>(); 
        public ObservableCollection<RelatedAnimeData> RelatedAnime { get;} = new ObservableCollection<RelatedAnimeData>(); 
        private readonly ObservableCollection<ListViewItem> _loadedItems1 = new ObservableCollection<ListViewItem>();
        private readonly ObservableCollection<ListViewItem> _loadedItems2 = new ObservableCollection<ListViewItem>();
        private readonly ObservableCollection<ListViewItem> _genres1 = new ObservableCollection<ListViewItem>();
        private readonly ObservableCollection<ListViewItem> _genres2 = new ObservableCollection<ListViewItem>();
        private readonly ObservableCollection<ListViewItem> _episodes = new ObservableCollection<ListViewItem>();     
        private readonly ObservableCollection<ListViewItem> _ops = new ObservableCollection<ListViewItem>();
        private readonly ObservableCollection<ListViewItem> _eds = new ObservableCollection<ListViewItem>();
        private int _allEpisodes;

        private string AnnId { get; set; }
        private int Id { get; set; }
        public string Title { get; set; }

        private int AllEpisodes
        {
            get
            {
                return _animeItemReference?.AllEpisodes ?? _allEpisodes;
            }
            set { _allEpisodes = value; }
        }

        public int AllVolumes => _animeItemReference.AllVolumes;

        private string Type { get; set; }
        private string Status { get; set; }
        private string _synopsis;
        

        private List<string> _synonyms = new List<string>();

        private string StartDate { get; set; }
        private string EndDate { get; set; }
        private bool _animeMode;
       
        public IDetailsViewInteraction View { get; set; }

        public DirectRecommendationData CurrentRecommendationsSelectedItem { get; set; }
        private AnimeDetailsPageNavigationArgs _prevArgs;

        #region Properties
        public string MyEpisodesBind => $"{MyEpisodes}/{(AllEpisodes == 0 ? "?" : AllEpisodes.ToString())}";
        private int MyEpisodes
        {
            get { return _animeItemReference?.MyEpisodes ?? 0; }
            set
            {
                _animeItemReference.MyEpisodes = value;
                RaisePropertyChanged(() => MyEpisodesBind);
            }
        }

        public string MyStatusBind => Utils.StatusToString(MyStatus,!_animeMode);
        private int MyStatus
        {
            get { return _animeItemReference?.MyStatus ?? (int)AnimeStatus.AllOrAiring; }
            set
            {
                _animeItemReference.MyStatus = value;
                RaisePropertyChanged(() => MyStatusBind);
            }
        }

        public string MyScoreBind => MyScore == 0 ? "Unranked" : $"{MyScore}/10";
        private int MyScore
        {
            get { return _animeItemReference?.MyScore ?? 0; }
            set
            {
                _animeItemReference.MyScore = value;
                RaisePropertyChanged(() => MyScoreBind);
            }
        }

        public string MyVolumesBind => MyVolumes.ToString();
        public int MyVolumes
        {
            get { return _animeItemReference?.MyVolumes ?? 0; }
            set
            {
                _animeItemReference.MyVolumes = value;
               
            }
        }

        private string _status1Label = "Watching";
        public string Status1Label
        {
            get { return _status1Label; }
            set
            {
                _status1Label = value;
                RaisePropertyChanged(() => Status1Label);
            }
        }

        private string _status5Label = "Plan to watch";
        public string Status5Label
        {
            get { return _status5Label; }
            set
            {
                _status5Label = value;
                RaisePropertyChanged(() => Status5Label);
            }
        }

        private string _watchedEpsLabel = "My watched\nepisodes :";
        public string WatchedEpsLabel
        {
            get { return _watchedEpsLabel; }
            set
            {
                _watchedEpsLabel = value;
                RaisePropertyChanged(() => WatchedEpsLabel);
            }
        }

        private string _updateEpsUpperLabel = "Watched eps :";
        public string UpdateEpsUpperLabel
        {
            get { return _updateEpsUpperLabel; }
            set
            {
                _updateEpsUpperLabel = value;
                RaisePropertyChanged(() => UpdateEpsUpperLabel);
            }
        }

        private bool _loadingUpdate;
        public bool LoadingUpdate
        {
            get { return _loadingUpdate; }
            set
            {
                _loadingUpdate = value;
                RaisePropertyChanged(() => LoadingUpdate);
            }
        }

        public string Synopsis
        {
            get { return _synopsis; }
            set
            {
                _synopsis = value;
                RaisePropertyChanged(() => Synopsis);
            }
        }

        private float GlobalScore
        {
            get { return _globalScore; }
            set
            {
                if (_animeItemReference != null)
                    _animeItemReference.GlobalScore = value;
                _globalScore = value;
            }
        }

        private Visibility _loadingGlobal;
        public Visibility LoadingGlobal
        {
            get { return _loadingGlobal; }
            set
            {
                _loadingGlobal = value;
                RaisePropertyChanged(() => LoadingGlobal);
            }
        }

        private Visibility _loadingDetails;
        public Visibility LoadingDetails
        {
            get { return _loadingDetails; }
            set
            {
                _loadingDetails = value;
                RaisePropertyChanged(() => LoadingDetails);
            }
        }

        private Visibility _loadingReviews;
        public Visibility LoadingReviews
        {
            get { return _loadingReviews; }
            set
            {
                _loadingReviews = value;
                RaisePropertyChanged(() => LoadingReviews);
            }
        }

        private Visibility _loadingRelated;
        public Visibility LoadingRelated
        {
            get { return _loadingRelated; }
            set
            {
                _loadingRelated = value;
                RaisePropertyChanged(() => LoadingRelated);
            }
        }

        private Visibility _loadingRecommendations;
        public Visibility LoadingRecommendations
        {
            get { return _loadingRecommendations; }
            set
            {
                _loadingRecommendations = value;
                RaisePropertyChanged(() => LoadingRecommendations);
            }
        }

        private Visibility _detailedDataVisibility;
        public Visibility DetailedDataVisibility
        {
            get { return _detailedDataVisibility; }
            set
            {
                _detailedDataVisibility = value;
                RaisePropertyChanged(() => DetailedDataVisibility);
            }
        }

        private string _watchedEpsInput;
        public string WatchedEpsInput {
            get { return _watchedEpsInput; }
            set
            {
                _watchedEpsInput = value;
                RaisePropertyChanged(() => WatchedEpsInput);
            } }

        private bool _watchedEpsInputNoticeVisibility;
        public bool WatchedEpsInputNoticeVisibility
        {
            get { return _watchedEpsInputNoticeVisibility; }
            set
            {
                _watchedEpsInputNoticeVisibility = value;
                RaisePropertyChanged(() => WatchedEpsInputNoticeVisibility);
            }
        }

        private bool _myDetailsVisibility;
        public bool MyDetailsVisibility
        {
            get { return _myDetailsVisibility; }
            set
            {
                _myDetailsVisibility = value;
                RaisePropertyChanged(() => MyDetailsVisibility);
            }
        }

        private bool _addAnimeVisibility;
        public bool AddAnimeVisibility
        {
            get { return _addAnimeVisibility; }
            set
            {
                _addAnimeVisibility = value;
                RaisePropertyChanged(() => AddAnimeVisibility);
            }
        }

        private ICommand _changeStatusCommand;
        public ICommand ChangeStatusCommand
        {
            get
            {
                return _changeStatusCommand ?? (_changeStatusCommand = new RelayCommand<object>(ChangeStatus));
            }
        }

        private ICommand _navigateDetailsCommand;
        public ICommand NavigateDetailsCommand
        {
            get
            {
                return _navigateDetailsCommand ?? (_navigateDetailsCommand = 
                    new RelayCommand<IDetailsPageArgs>(args => NavigateDetails(args)));
            }
        }

        private ICommand _changeScoreCommand;
        public ICommand ChangeScoreCommand
        {
            get
            {
                return _changeScoreCommand ?? (_changeScoreCommand = new RelayCommand<Object>(ChangeScore));
            }
        }

        private ICommand _changeWatchedCommand;
        public ICommand ChangeWatchedCommand
        {
            get
            {
                return _changeWatchedCommand ?? (_changeWatchedCommand = new RelayCommand(ChangeWatchedEps));
            }
        }

        private ICommand _addAnimeCommand;
        public ICommand AddAnimeCommand
        {
            get
            {
                return _addAnimeCommand ?? (_addAnimeCommand = new RelayCommand(AddAnime));
            }
        }

        private ICommand _removeAnimeCommand;
        public ICommand RemoveAnimeCommand
        {
            get
            {
                return _removeAnimeCommand ?? (_removeAnimeCommand = new RelayCommand(RemoveAnime));
            }
        }

        private ICommand _openInMalCommand;
        public ICommand OpenInMalCommand
        {
            get
            {
                return _openInMalCommand ?? (_openInMalCommand = new RelayCommand(OpenMalPage));
            }
        }

        private ICommand _openInAnnCommand;
        public ICommand OpenInAnnCommand
        {
            get
            {
                return _openInAnnCommand ?? (_openInAnnCommand = new RelayCommand(OpenAnnPage));
            }
        }

        private BitmapImage _detailImage;
        public BitmapImage DetailImage
        {
            get { return _detailImage; }
            set
            {
                _detailImage = value;
                RaisePropertyChanged(() => DetailImage);
            }
        }

        private Visibility _noEpisodesDataVisibility;
        public Visibility NoEpisodesDataVisibility
        {
            get { return _noEpisodesDataVisibility; }
            set
            {
                _noEpisodesDataVisibility = value;
                RaisePropertyChanged(() => NoEpisodesDataVisibility);
            }
        }

        private Visibility _noEDsDataVisibility;
        public Visibility NoEDsDataVisibility
        {
            get { return _noEDsDataVisibility; }
            set
            {
                _noEDsDataVisibility = value;
                RaisePropertyChanged(() => NoEDsDataVisibility);
            }
        }

        private Visibility _noOPsDataVisibility;
        public Visibility NoOPsDataVisibility
        {
            get { return _noOPsDataVisibility; }
            set
            {
                _noOPsDataVisibility = value;
                RaisePropertyChanged(() => NoOPsDataVisibility);
            }
        }

        private Visibility _noGenresDataVisibility;
        public Visibility NoGenresDataVisibility
        {
            get { return _noGenresDataVisibility; }
            set
            {
                _noGenresDataVisibility = value;
                RaisePropertyChanged(() => NoGenresDataVisibility);
            }
        }

        private Visibility _annSourceButtonVisibility;
        public Visibility AnnSourceButtonVisibility
        {
            get { return _annSourceButtonVisibility; }
            set
            {
                _annSourceButtonVisibility = value;
                RaisePropertyChanged(() => AnnSourceButtonVisibility);
            }
        }

        private Visibility _pivotItemDetailsVisibility = Visibility.Visible;
        public Visibility PivotItemDetailsVisibility
        {
            get { return _pivotItemDetailsVisibility; }
            set
            {
                _pivotItemDetailsVisibility = value;
                RaisePropertyChanged(() => PivotItemDetailsVisibility);
            }
        }

        private int _detailsPivotSelectedIndex;
        public int DetailsPivotSelectedIndex
        {
            get { return _detailsPivotSelectedIndex; }
            set
            {
                _detailsPivotSelectedIndex = value;
                RaisePropertyChanged(() => DetailsPivotSelectedIndex);
            }
        }

        private Visibility _noReviewsDataNoticeVisibility = Visibility.Collapsed;
        public Visibility NoReviewsDataNoticeVisibility
        {
            get { return _noReviewsDataNoticeVisibility; }
            set
            {
                _noReviewsDataNoticeVisibility = value;
                RaisePropertyChanged(() => NoReviewsDataNoticeVisibility);
            }
        }

        private Visibility _noRecommDataNoticeVisibility = Visibility.Collapsed;
        public Visibility NoRecommDataNoticeVisibility
        {
            get { return _noRecommDataNoticeVisibility; }
            set
            {
                _noRecommDataNoticeVisibility = value;
                RaisePropertyChanged(() => NoRecommDataNoticeVisibility);
            }
        }

        private Visibility _noRelatedDataNoticeVisibility = Visibility.Collapsed;
        public Visibility NoRelatedDataNoticeVisibility
        {
            get { return _noRelatedDataNoticeVisibility; }
            set
            {
                _noRelatedDataNoticeVisibility = value;
                RaisePropertyChanged(() => NoRelatedDataNoticeVisibility);
            }
        }

        
#endregion

        public async void Init(AnimeDetailsPageNavigationArgs param)
        {
            LoadingGlobal = Visibility.Visible;
            _animeMode = param.AnimeMode;
            PivotItemDetailsVisibility = _animeMode ? Visibility.Visible : Visibility.Collapsed;
            _prevArgs = param;
            Id = param.Id;
            Title = param.Title;
            _animeItemReference = param.AnimeItem;

            if (_animeMode)
            {
                Status1Label = "Watching";
                Status5Label = "Plan to watch";
                WatchedEpsLabel = "My watched\nepisodes :";
                UpdateEpsUpperLabel = "Watched eps :";
            }
            else
            {
                Status1Label = "Reading";
                Status5Label = "Plan to read";
                WatchedEpsLabel = "My read\nchapters :";
                UpdateEpsUpperLabel = "Read chapters : ";
            }

            if (_animeItemReference == null || _animeItemReference is AnimeSearchItem || !(_animeItemReference as AnimeItemViewModel).Auth)
            //if we are from search or from unauthenticated item let's look for proper abstraction
            {
                if (!ViewModelLocator.AnimeList.TryRetrieveAuthenticatedAnimeItem(param.Id, ref _animeItemReference,_animeMode))
                // else we don't have this item
                {
                    //we may only prepare for its creation
                    AddAnimeVisibility = true;
                    MyDetailsVisibility = false;
                }
            } // else we already have it

            if (_animeItemReference is AnimeItemViewModel && (_animeItemReference as AnimeItemViewModel).Auth)
            {
                //we have item on the list , so there's valid data here
                MyDetailsVisibility = true;
                AddAnimeVisibility = false;
                RaisePropertyChanged(() => MyEpisodesBind);
                RaisePropertyChanged(() => MyStatusBind);
                RaisePropertyChanged(() => MyScoreBind);
            }

            switch (param.Source)
            {
                case PageIndex.PageSearch:
                case PageIndex.PageMangaSearch:
                    ExtractData(param.AnimeElement);
                    NavMgr.RegisterBackNav(param.Source, param.PrevPageSetup);
                    break;
                case PageIndex.PageAnimeList:
                case PageIndex.PageMangaList:
                    await FetchData(param.Id.ToString(), param.Title);
                    NavMgr.RegisterBackNav(param.Source, param.PrevPageSetup);
                    break;
                case PageIndex.PageAnimeDetails:
                    await FetchData(param.Id.ToString(), param.Title);
                    if(param.RegisterBackNav) //we are already going back
                        NavMgr.RegisterBackNav(param.Source, param.PrevPageSetup , PageIndex.PageAnimeDetails);
                    break;
                case PageIndex.PageRecomendations:
                    ExtractData(param.AnimeElement);
                    NavMgr.RegisterBackNav(param.Source, param.PrevPageSetup);
                    break;
            }
            _loadedDetails = _loadedReviews = _loadedRecomm = _loadedRelated = false;
            DetailsPivotSelectedIndex = 0;
            Reviews.Clear();
        }

        private async void OpenMalPage()
        {
            await Launcher.LaunchUriAsync(new Uri($"http://myanimelist.net/{(_animeMode ? "anime" : "manga")}/{Id}"));
        }

        private async void OpenAnnPage()
        {
            await Launcher.LaunchUriAsync(new Uri($"http://www.animenewsnetwork.com/encyclopedia/anime.php?id={AnnId}"));
        }

        #region ChangeStuff

        private Query GetAppropriateUpdateQuery()
        {
            if (_animeMode)
                return new AnimeUpdateQuery(Id, MyEpisodes, MyStatus, MyScore);
            return new MangaUpdateQuery(Id, MyEpisodes, MyStatus, MyScore, MyVolumes);
        }

        private async void ChangeStatus(object status)
        {
            LoadingUpdate = true;
            var prevStatus = MyStatus;
            MyStatus = Utils.StatusToInt(status as string);
            var response = await GetAppropriateUpdateQuery().GetRequestResponse();
            if (response != "Updated")
                MyStatus = prevStatus;

            if (_animeItemReference is AnimeItemViewModel)
                if (MyStatus == (int) AnimeStatus.Completed && MyEpisodes != AllEpisodes && AllEpisodes != 0)
                {
                    await ((AnimeItemViewModel) _animeItemReference).PromptForWatchedEpsChange(AllEpisodes);
                    RaisePropertyChanged(() => MyEpisodesBind);
                }


            LoadingUpdate = false;
        }

        private async void ChangeScore(object score)
        {
            LoadingUpdate = true;
            var prevScore = MyScore;
            MyScore = Convert.ToInt32(score as string);
            var response = await GetAppropriateUpdateQuery().GetRequestResponse();
            if (response != "Updated")
                MyScore = prevScore;
            LoadingUpdate = false;
        }

        public async void ChangeWatchedEps()
        {
            LoadingUpdate = true;
            int eps,prevEps = MyEpisodes;
            if (!int.TryParse(WatchedEpsInput, out eps))
            {
                WatchedEpsInputNoticeVisibility = true;
                LoadingUpdate = false;
                return;
            }
            if (eps >= 0 && (AllEpisodes == 0 || eps <= AllEpisodes))
            {
                View.GetWatchedEpsFlyout().Hide();
                WatchedEpsInputNoticeVisibility = false;
                var prevWatched = MyEpisodes;
                MyEpisodes = eps;
                var response = await GetAppropriateUpdateQuery().GetRequestResponse();
                if (response != "Updated")
                    MyEpisodes = prevWatched;

                if(_animeItemReference is AnimeItemViewModel)
                    if (prevEps == 0 && AllEpisodes != 0 && MyEpisodes != AllEpisodes &&
                        (MyStatus == (int) AnimeStatus.PlanToWatch || MyStatus == (int) AnimeStatus.Dropped ||
                         MyStatus == (int) AnimeStatus.OnHold))
                    {
                        await ((AnimeItemViewModel) _animeItemReference).PromptForStatusChange((int)AnimeStatus.Watching);
                        RaisePropertyChanged(() => MyStatusBind);
                    }
                    else if (MyEpisodes == AllEpisodes && AllEpisodes != 0)
                    {
                        await ((AnimeItemViewModel) _animeItemReference).PromptForStatusChange((int)AnimeStatus.Completed);
                        RaisePropertyChanged(() => MyStatusBind);
                    }
                WatchedEpsInput = "";
            }
            else
            {
                WatchedEpsInputNoticeVisibility = true;
            }
            LoadingUpdate = false;
        }



        #endregion

        #region Add/Remove

        private async void AddAnime()
        {
            var response = _animeMode
                ? await new AnimeAddQuery(Id.ToString()).GetRequestResponse()
                : await new MangaAddQuery(Id.ToString()).GetRequestResponse();
            if (!response.Contains("Created") && _animeMode)
                return;
            AddAnimeVisibility = false;
            var animeItem = _animeMode
                ? new AnimeItemAbstraction(true, Title, _imgUrl, Id, 6, 0, AllEpisodes, 0)
                : new AnimeItemAbstraction(true, Title, _imgUrl, Id, 6, 0, AllEpisodes, 0,0, AllVolumes);
            _animeItemReference = animeItem.ViewModel;
            MyScore = 0;
            MyStatus = 6;
            MyEpisodes = 0;
            GlobalScore = GlobalScore; //trigger setter of anime item
            if (Status == "Currently Airing")
                (_animeItemReference as AnimeItemViewModel).Airing = true;
            ViewModelLocator.AnimeList.AddAnimeEntry(animeItem);
            MyDetailsVisibility = true;
        }

        private async void RemoveAnime()
        {
            var uSure = false;
            var msg = new MessageDialog("Are you sure about deleting this entry from your list?");
            msg.Commands.Add(new UICommand("I'm sure", command => uSure = true));
            msg.Commands.Add(new UICommand("Cancel", command => uSure = false));
            await msg.ShowAsync();
            if (!uSure)
                return;

            var response = _animeMode
                ? await new AnimeRemoveQuery(Id.ToString()).GetRequestResponse()
                : await new MangaRemoveQuery(Id.ToString()).GetRequestResponse();
            if (!response.Contains("Deleted"))
                return;

            ViewModelLocator.AnimeList.RemoveAnimeEntry((_animeItemReference as AnimeItemViewModel)._parentAbstraction);

            (_animeItemReference as AnimeItemViewModel).SetAuthStatus(false, true);
            AddAnimeVisibility = true;
            MyDetailsVisibility = false;
        }

        #endregion

        #region FetchAndPopulate

        private void PopulateData()
        {
            if (_animeItemReference is AnimeItemViewModel && _animeMode)
            {
                var day = Status == "Currently Airing" ? (int)DateTime.Parse(StartDate).DayOfWeek + 1 : -1;
                DataCache.RegisterVolatileData(Id, new VolatileDataCache
                {
                    DayOfAiring = day,
                    GlobalScore = GlobalScore
                });
                ((AnimeItemViewModel)_animeItemReference).Airing = day != -1;
                DataCache.SaveVolatileData();
            }
            _loadedItems1.Clear();
            _loadedItems2.Clear();
            _loadedItems1.Add(BuildListViewItem("Episodes", AllEpisodes == 0 ? "?" : AllEpisodes.ToString(), true));
            _loadedItems1.Add(BuildListViewItem("Score", GlobalScore.ToString()));
            _loadedItems1.Add(BuildListViewItem("Start", StartDate == "0000-00-00" ? "?" : StartDate, true));
            _loadedItems2.Add(BuildListViewItem("Type", Type, true, 0.3f, 0.7f));
            _loadedItems2.Add(BuildListViewItem("Status", Status, false, 0.3f, 0.7f));
            _loadedItems2.Add(BuildListViewItem("End", EndDate == "0000-00-00" ? "?" : EndDate, true, 0.3f, 0.7f));

            Synopsis = Synopsis;
            Utils.GetMainPageInstance().CurrentStatus = Title;

            DetailImage  = new BitmapImage(new Uri(_imgUrl));
            LoadingGlobal = Visibility.Collapsed;
        }

        private ListViewItem BuildListViewItem(string label, string val1, bool alternate = false, float left = 0.4f, float right = 0.6f)
        {
            return new ListViewItem
            {
                Content = new Grid
                {
                    ColumnDefinitions =
                    {
                        new ColumnDefinition {Width = new GridLength(left,GridUnitType.Star)},
                        new ColumnDefinition {Width = new GridLength(right,GridUnitType.Star)}
                    },
                    Children =
                    {
                        BuildTextBlock(label,val1 == "" ? FontWeights.SemiLight :FontWeights.SemiBold,0),
                        BuildTextBlock(val1,FontWeights.SemiLight,1),
                    },
                },
                Background = new SolidColorBrush(alternate ? Color.FromArgb(170, 230, 230, 230) : Color.FromArgb(255, 245, 245, 245)),
                Height = 20
            };
        }

        private TextBlock BuildTextBlock(string value, FontWeight weight, int column)
        {
            var txt = new TextBlock
            {
                Text = value,
                FontWeight = weight,
                FontSize = 13,
                Height = 20,
                TextAlignment = !weight.Equals(FontWeights.SemiBold) ? TextAlignment.Center : TextAlignment.Left
            };
            txt.SetValue(Grid.ColumnProperty, column);
            return txt;
        }


        private void ExtractData(XElement animeElement)
        {
            GlobalScore = float.Parse(animeElement.Element("score").Value);
            Type = animeElement.Element("type").Value;
            Status = animeElement.Element("status").Value;
            Synopsis = Utils.DecodeXmlSynopsisDetail(animeElement.Element("synopsis").Value);               
            StartDate = animeElement.Element("start_date").Value;
            EndDate = animeElement.Element("end_date").Value;
            _imgUrl = animeElement.Element("image").Value;
            _synonyms = animeElement.Element("synonyms").Value.Split(',').ToList();
            _synonyms.Add(animeElement.Element("english").Value);
            _synonyms.Add(Title);
            _synonyms = _synonyms.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            for(int i=0;i<_synonyms.Count;i++)
                _synonyms[i] = Regex.Replace(_synonyms[i], @" ?\(.*?\)", string.Empty); //removes string from brackets (sthsth) lol ->  lol
            if (_animeItemReference == null)
                AllEpisodes = Convert.ToInt32(animeElement.Element(_animeMode ? "episodes" : "chapters").Value);
            PopulateData();
        }

        private async Task FetchData(string id, string title)
        {
            LoadingGlobal = Visibility.Visible;
            var data = "";
            await Task.Run(async () => data = _animeMode  
                 ? await new AnimeSearchQuery(Utils.CleanAnimeTitle(title)).GetRequestResponse()
                 : await new MangaSearchQuery(Utils.CleanAnimeTitle(title)).GetRequestResponse());
            data = WebUtility.HtmlDecode(data);
            data = data.Replace("&mdash", "").Replace("&rsquo", "").Replace("&", "");

            XDocument parsedData = XDocument.Parse(data);
            var elements = parsedData.Element(_animeMode ? "anime" : "manga").Elements("entry");
            ExtractData(elements.First(element => element.Element("id").Value == id));
        }

        public async void RefreshData()
        {
            await FetchData(Id.ToString(),Title);
            if(_loadedDetails)
                LoadDetails(true);
            if(_loadedReviews)
                LoadReviews(true);
            if (_loadedRecomm)
                LoadRecommendations(true);
        }

        public async void LoadDetails(bool force = false)
        {
            if (_loadedDetails && !force)
                return;
            _loadedDetails = true;
            LoadingDetails = Visibility.Visible;
            _genres1.Clear();
            _genres2.Clear();
            _episodes.Clear();
            _ops.Clear();
            _eds.Clear();
            try
            {
                var data = await new AnimeGeneralDetailsQuery(_synonyms.Count == 1 ? Title : string.Join("&title=~",_synonyms),Id,Title).GetGeneralDetailsData(force);
                AnnId = data.AnnId;

                //Let's try to pull moar Genres data from MAL
                VolatileDataCache genresData;
                if (DataCache.TryRetrieveDataForId(Id, out genresData) && genresData.Genres != null)
                {
                    foreach (var genreMal in genresData.Genres)
                    {
                        if (data.Genres.All(genreAnn => !String.Equals(genreAnn, genreMal, StringComparison.CurrentCultureIgnoreCase)))
                        {
                            data.Genres.Add(genreMal);
                        }
                    }
                }
                //Now we can build elements here --- TODO: I know know , I shoud've used data templates here but I learned about them a bit later ^^ TODO : Refactor This
                int i = 1;
                bool alternate1 = true, alternate2 = true;
                foreach (var genre in data.Genres)
                {
                    if (i % 2 == 0)
                    {
                        _genres1.Add(BuildListViewItem(Utils.FirstCharToUpper(genre),"", alternate1, 1f, 0f));
                        alternate1 = !alternate1;
                    }
                    else
                    {
                        _genres2.Add(BuildListViewItem(Utils.FirstCharToUpper(genre), "", alternate2, 1f, 0f));
                        alternate2 = !alternate2;
                    }
                    i++;
                }
                i = 1;
                alternate1 = false;
                foreach (var episode in data.Episodes.Take(40))
                {
                        _episodes.Add(BuildListViewItem($"{i++}.", episode, alternate1, 0.1f, 0.9f));
                        alternate1 = !alternate1;
                }
                if (data.Episodes.Count > 40)
                {
                    _episodes.Add(BuildListViewItem("?.", $"{data.Episodes.Count - 40} More episodes...", alternate1, 0.1f, 0.9f));
                }
                i = 1;
                alternate1 = true;
                foreach (var op in data.OPs)
                {
                    _ops.Add(BuildListViewItem(op,"", alternate1, 1f, 0f));
                    alternate1 = !alternate1;
                    i++;
                }
                i = 1;
                alternate1 = true;
                foreach (var ed in data.EDs)
                {
                    _eds.Add(BuildListViewItem( ed,"", alternate1, 1f, 0f));
                    alternate1 = !alternate1;
                    i++;
                }
                
                DetailedDataVisibility = Visibility.Visible;
                AnnSourceButtonVisibility = Visibility.Visible;
            }
            catch (Exception)
            {
                VolatileDataCache genresData; // we may fail to pull genres from ann so we have this from MAL season page 
                if (DataCache.TryRetrieveDataForId(Id, out genresData))
                {
                    AnnSourceButtonVisibility = Visibility.Collapsed;
                    DetailedDataVisibility = Visibility.Visible;

                    int i = 1;
                    bool alternate1 = true, alternate2 = true;
                    foreach (var genre in genresData.Genres ?? new List<string>())
                    {
                        if (i % 2 == 0)
                        {
                            _genres1.Add(BuildListViewItem(Utils.FirstCharToUpper(genre), "", alternate1, 1f, 0f));
                            alternate1 = !alternate1;
                        }
                        else
                        {
                            _genres2.Add(BuildListViewItem(Utils.FirstCharToUpper(genre), "", alternate2, 1f, 0f));
                            alternate2 = !alternate2;
                        }
                        i++;
                    }
                }
                else
                    DetailedDataVisibility = Visibility.Collapsed;
            }
            NoEpisodesDataVisibility = _episodes.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            NoGenresDataVisibility = _genres1.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            NoEDsDataVisibility = _eds.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            NoOPsDataVisibility = _ops.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            if(_episodes.Count == 0 && _genres1.Count == 0 && _eds.Count == 0 && _ops.Count==0)
                DetailedDataVisibility = Visibility.Collapsed;
                       
            LoadingDetails = Visibility.Collapsed;
        }

        public async void LoadReviews(bool force = false)
        {
            if(_loadedReviews && !force)
                return;
            LoadingReviews = Visibility.Visible;
            _loadedReviews = true;
            Reviews.Clear();
            List<AnimeReviewData> revs = new List<AnimeReviewData>();
            await Task.Run( async () => revs = await new AnimeReviewsQuery(Id,_animeMode).GetAnimeReviews(force));
            
            foreach (var rev in revs)
            {
                Reviews.Add(rev);
            }
            NoReviewsDataNoticeVisibility = Reviews.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
            LoadingReviews = Visibility.Collapsed;
        }

        public async void LoadRecommendations(bool force = false)
        {
            if (_loadedRecomm && !force)
                return;
            LoadingRecommendations = Visibility.Visible;
            _loadedRecomm = true;
            Recommendations.Clear();
            var recomm = new List<DirectRecommendationData>();
            await Task.Run(async () => recomm = await new AnimeDirectRecommendationsQuery(Id,_animeMode).GetDirectRecommendations(force));
            foreach (var item in recomm)
                Recommendations.Add(item);
            NoRecommDataNoticeVisibility = Recommendations.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
            LoadingRecommendations = Visibility.Collapsed;
        }

        public async void LoadRelatedAnime(bool force = false)
        {
            if (_loadedRelated && !force)
                return;
            LoadingRelated = Visibility.Visible;
            _loadedRelated = true;
            RelatedAnime.Clear();
            var recomm = new List<RelatedAnimeData>();
            await Task.Run(async () => recomm = await new AnimeRelatedQuery(Id,_animeMode).GetRelatedAnime(force));
            foreach (var item in recomm)
                RelatedAnime.Add(item);
            NoRelatedDataNoticeVisibility = RelatedAnime.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
            LoadingRelated = Visibility.Collapsed;
        }
        #endregion

        public async void NavigateDetails(IDetailsPageArgs args)
        {
            await ViewModelLocator.Main
                .Navigate(PageIndex.PageAnimeDetails,
                    new AnimeDetailsPageNavigationArgs(args.Id, args.Title , null, null,
                        new AnimeDetailsPageNavigationArgs(Id, Title, null , _animeItemReference)
                        { Source = PageIndex.PageAnimeDetails, RegisterBackNav = false , AnimeMode = _animeMode})
                    {Source = PageIndex.PageAnimeDetails , AnimeMode = args.Type == RelatedItemType.Anime});
        }
    }
}
