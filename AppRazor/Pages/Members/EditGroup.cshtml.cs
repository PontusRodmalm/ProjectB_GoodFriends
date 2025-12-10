using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using AppRazor.SeidoHelpers;
using Newtonsoft.Json;
using Services;
using Models.DTO;
using Models.Interfaces;

namespace AppRazor.Pages
{
    public class EditGroupModel : PageModel
    {
        //Just like for WebApi
        readonly IMusicGroupsService _mg_service = null;
        readonly IAlbumsService _alb_service = null;
        readonly IArtistsService _art_service = null;
        readonly ILogger<EditGroupModel> _logger = null;

        //InputModel (IM) is locally declared classes that contains ONLY the properties of the Model
        //that are bound to the <form> tag
        //EVERY property must be bound to an <input> tag in the <form>
        [BindProperty]
        public MusicGroupIM MusicGroupInput { get; set; }

        //I also use BindProperty to keep between several posts, bound to hidden <input> field
        [BindProperty]
        public string PageHeader { get; set; }

        //Used to populate the dropdown select
        //Notice how it will be populate every time the class is instansiated, i.e. before every get and post
        public List<SelectListItem> GenreItems { set; get; } = new List<SelectListItem>().PopulateSelectList<MusicGenre>();

        //For Validation
        public ModelValidationResult ValidationResult { get; set; } = new ModelValidationResult(false, null, null);

        #region HTTP Requests
        public async Task<IActionResult> OnGet()
        {
            if (Guid.TryParse(Request.Query["id"], out Guid _groupId))
            {
                //Read a music group from 
                var mg = await _mg_service.ReadMusicGroupAsync(_groupId, false);

                //Populate the InputModel from the music group
                MusicGroupInput = new MusicGroupIM(mg.Item);
                PageHeader = "Edit details of a music group";

            }
            else
            {
                //Create an empty music group
                MusicGroupInput = new MusicGroupIM();
                MusicGroupInput.StatusIM = StatusIM.Inserted;
                MusicGroupInput.Genre = null;

                PageHeader = "Create a new a music group";
            }

            return Page();
        }

        public IActionResult OnPostDeleteArtist(Guid artistId)
        {
            //Set the Artist as deleted, it will not be rendered
            MusicGroupInput.Artists.First(a => a.ArtistId == artistId).StatusIM = StatusIM.Deleted;

            return Page();
        }

        public IActionResult OnPostAddArtist()
        {
            string[] keys = { "MusicGroupInput.NewArtist.FirstName",
                              "MusicGroupInput.NewArtist.LastName"};

            if (!ModelState.IsValidPartially(out ModelValidationResult validationResult, keys))
            {
                ValidationResult = validationResult;
                return Page();
            }

            //Set the Artist as Inserted, it will later be inserted in the database
            MusicGroupInput.NewArtist.StatusIM = StatusIM.Inserted;

            //Need to add a temp Guid so it can be deleted and editited in the form
            //A correct Guid will be created by the DTO when Inserted into the database
            MusicGroupInput.NewArtist.ArtistId = Guid.NewGuid();

            //Add it to the Input Models artists
            MusicGroupInput.Artists.Add(new ArtistIM(MusicGroupInput.NewArtist));

            //Clear the NewArtist so another album can be added
            MusicGroupInput.NewArtist = new ArtistIM();

            return Page();
        }

        public IActionResult OnPostEditArtist(Guid artistId)
        {
            int idx = MusicGroupInput.Artists.FindIndex(a => a.ArtistId == artistId);
            string[] keys = { $"MusicGroupInput.Artists[{idx}].editFirstName",
                            $"MusicGroupInput.Artists[{idx}].editLastName"};

            if (!ModelState.IsValidPartially(out ModelValidationResult validationResult, keys))
            {
                ValidationResult = validationResult;
                return Page();
            }

            //Set the Album as Modified, it will later be updated in the database
            var a = MusicGroupInput.Artists.First(a => a.ArtistId == artistId);
            if (a.StatusIM != StatusIM.Inserted)
            {
                a.StatusIM = StatusIM.Modified;
            }

            //Implement the changes
            a.FirstName = a.editFirstName;
            a.LastName = a.editLastName;

            return Page();
        }


        public IActionResult OnPostDeleteAlbum(Guid albumId)
        {
            //Set the Album as deleted, it will not be rendered
            MusicGroupInput.Albums.First(a => a.AlbumId == albumId).StatusIM = StatusIM.Deleted;

            return Page();
        }

        public IActionResult OnPostAddAlbum()
        {
            string[] keys = { "MusicGroupInput.NewAlbum.ReleaseYear",
                              "MusicGroupInput.NewAlbum.AlbumName"};

            if (!ModelState.IsValidPartially(out ModelValidationResult validationResult, keys))
            {
                ValidationResult = validationResult;
                return Page();
            }

            //Set the Album as Inserted, it will later be inserted in the database
            MusicGroupInput.NewAlbum.StatusIM = StatusIM.Inserted;

            //Need to add a temp Guid so it can be deleted and editited in the form
            //A correct Guid will be created by the DTO when Inserted into the database
            MusicGroupInput.NewAlbum.AlbumId = Guid.NewGuid();

            //Add it to the Input Models albums
            MusicGroupInput.Albums.Add(new AlbumIM(MusicGroupInput.NewAlbum));

            //Clear the NewAlbum so another album can be added
            MusicGroupInput.NewAlbum = new AlbumIM();

            return Page();
        }

        public IActionResult OnPostEditAlbum(Guid albumId)
        {
            int idx = MusicGroupInput.Albums.FindIndex(a => a.AlbumId == albumId);
            string[] keys = { $"MusicGroupInput.Albums[{idx}].editAlbumName",
                            $"MusicGroupInput.Albums[{idx}].editReleaseYear"};

            if (!ModelState.IsValidPartially(out ModelValidationResult validationResult, keys))
            {
                ValidationResult = validationResult;
                return Page();
            }

            //Set the Album as Modified, it will later be updated in the database
            var a = MusicGroupInput.Albums.First(a => a.AlbumId == albumId);
            if (a.StatusIM != StatusIM.Inserted)
            {
                a.StatusIM = StatusIM.Modified;
            }

            //Implement the changes
            a.AlbumName = a.editAlbumName;
            a.ReleaseYear = a.editReleaseYear;

            return Page();
        }

        public async Task<IActionResult> OnPostUndo()
        {
            //Reload Music group from Database
            var mg = await _mg_service.ReadMusicGroupAsync(MusicGroupInput.MusicGroupId, false);

            //Repopulate the InputModel
            MusicGroupInput = new MusicGroupIM(mg.Item);
            return Page();
        }

        public async Task<IActionResult> OnPostSave()
        {
            string[] keys = { "MusicGroupInput.Name",
                              "MusicGroupInput.EstablishedYear",
                              "MusicGroupInput.Genre"};

            if (!ModelState.IsValidPartially(out ModelValidationResult validationResult, keys))
            {
                ValidationResult = validationResult;
                return Page();
            }

            //This is where the music plays
            //First, are we creating a new Music group or editing another
            if (MusicGroupInput.StatusIM == StatusIM.Inserted)
            {
                var newMg = await _mg_service.CreateMusicGroupAsync(MusicGroupInput.CreateCUdto());
                //get the newly created MusicGroupId
                MusicGroupInput.MusicGroupId = newMg.Item.MusicGroupId;
            }

            //Do all updates for Albums
            await SaveAlbums();

            // Do all updates for Artists
            var mg = await SaveArtists();

            //Finally, update the MusicGroup itself
            mg = MusicGroupInput.UpdateModel(mg);
            await _mg_service.UpdateMusicGroupAsync(new MusicGroupCUdto(mg));

            if (MusicGroupInput.StatusIM == StatusIM.Inserted)
            {
                return Redirect($"~/ListOfGroups");
            }

            return Redirect($"~/ViewGroup?id={MusicGroupInput.MusicGroupId}");
        }
        #endregion

        #region InputModel Albums and Artists saved to database
        private async Task<IMusicGroup> SaveAlbums()
        {
            //Check if there are deleted albums, if so simply remove them
            var deletedAlbums = MusicGroupInput.Albums.FindAll(a => (a.StatusIM == StatusIM.Deleted));
            foreach (var item in deletedAlbums)
            {
                //Remove from the database
                await _alb_service.DeleteAlbumAsync(item.AlbumId);
            }

            //Note that now the deleted albums will be removed and I can focus on Album creation
            await _mg_service.ReadMusicGroupAsync(MusicGroupInput.MusicGroupId, false);

            //Check if there are any new albums added, if so create them in the database
            var newAlbums = MusicGroupInput.Albums.FindAll(a => (a.StatusIM == StatusIM.Inserted));
            foreach (var item in newAlbums)
            {
                //Create the corresposning model and CUdto objects
                var cuDto = item.CreateCUdto();

                //Set the relationships of a newly created item and write to database
                cuDto.MusicGroupId = MusicGroupInput.MusicGroupId;
                await _alb_service.CreateAlbumAsync(cuDto);
            }

            //Note that now the deleted albums will be removed and created albums added. I can focus on Album update
            var mg = await _mg_service.ReadMusicGroupAsync(MusicGroupInput.MusicGroupId, false);

            //Check if there are any modified albums , if so update them in the database
            var modifiedAlbums = MusicGroupInput.Albums.FindAll(a => (a.StatusIM == StatusIM.Modified));
            foreach (var item in modifiedAlbums)
            {
                var model = mg.Item.Albums.First(a => a.AlbumId == item.AlbumId);

                //Update the model from the InputModel
                model = item.UpdateModel(model);

                //Updatet the model in the database
                model.MusicGroup = mg.Item;      //ensure that MusicGroupId can be set, Album must belong to a music group
                await _alb_service.UpdateAlbumAsync(new AlbumCUdto(model));
            }

            return mg.Item;
        }
        private async Task<IMusicGroup> SaveArtists()
        {
            //Check if there are deleted artists, if so simply remove them
            var deletedArtists = MusicGroupInput.Artists.FindAll(a => (a.StatusIM == StatusIM.Deleted));
            foreach (var item in deletedArtists)
            {
                //Remove from the database
                await _art_service.DeleteArtistAsync(item.ArtistId);
            }

            //Check if there are any new artist added, if so create them in the database
            var newArtists = MusicGroupInput.Artists.FindAll(a => (a.StatusIM == StatusIM.Inserted));
            foreach (var item in newArtists)
            {
                //Create the corresposning model and CUdto objects
                var cuDto = item.CreateCUdto();

                //Set the relationships of a newly created item and write to database
                cuDto.MusicGroupsId = [MusicGroupInput.MusicGroupId];

                //Create if does not exists. 
                await _art_service.CreateArtistAsync(cuDto);
            }

            //To update modified and deleted Artists, lets first read the original
            //Note that now the deleted artists will be removed and created artists will be nicely included
            var mg = await _mg_service.ReadMusicGroupAsync(MusicGroupInput.MusicGroupId, false);


            //Check if there are any modified artists , if so update them in the database
            var modifiedArtists = MusicGroupInput.Artists.FindAll(a => (a.StatusIM == StatusIM.Modified));
            foreach (var item in modifiedArtists)
            {
                var model = mg.Item.Artists.First(a => a.ArtistId == item.ArtistId);

                //Update the model from the InputModel
                model = item.UpdateModel(model);

                //Updatet the model in the database
                await _art_service.UpdateArtistAsync(new ArtistCUdto(model));
            }

            return mg.Item;
        }
        #endregion

        #region Constructors
        //Inject services just like in WebApi
        public EditGroupModel(IMusicGroupsService mg_service, IAlbumsService alb_service,
                              IArtistsService art_service, ILogger<EditGroupModel> logger)
        {
            _mg_service = mg_service;
            _alb_service = alb_service;
            _art_service = art_service;
            _logger = logger;
        }
        #endregion

        #region Input Model
        //InputModel (IM) is locally declared classes that contains ONLY the properties of the Model
        //that are bound to the <form> tag
        //EVERY property must be bound to an <input> tag in the <form>
        //These classes are in center of ModelBinding and Validation
        public enum StatusIM { Unknown, Unchanged, Inserted, Modified, Deleted}
        public class ArtistIM
        {
            public StatusIM StatusIM { get; set; }

            public Guid ArtistId { get; set; }

            [Required(ErrorMessage = "You must provide a first name")]
            public string FirstName { get; set; }

            [Required(ErrorMessage = "You must provide a last name")]
            public string LastName { get; set; }

            //This is because I want to confirm modifications in PostEditAlbum
            [Required(ErrorMessage = "You must provide a first name")]
            public string editFirstName { get; set; }

            [Required(ErrorMessage = "You must provide a last name")]
            public string editLastName { get; set; }

            public ArtistIM() { }
            public ArtistIM(ArtistIM original)
            {
                StatusIM = original.StatusIM;
                ArtistId = original.ArtistId;
                FirstName = original.FirstName;
                LastName = original.LastName;

                editFirstName = original.editFirstName;
                editLastName = original.editLastName;
            }
            public ArtistIM(IArtist model)
            {
                StatusIM = StatusIM.Unchanged;
                ArtistId = model.ArtistId;
                FirstName = editFirstName = model.FirstName;
                LastName = editLastName = model.LastName;
            }
            
            //to update the model in database
            public IArtist UpdateModel(IArtist model)
            {
                model.ArtistId = this.ArtistId;
                model.FirstName = this.FirstName;
                model.LastName = this.LastName;
                return model;
            }

            //to create new artist in the database
            public ArtistCUdto CreateCUdto () => new ArtistCUdto(){

                ArtistId = null,
                FirstName = this.FirstName,
                LastName = this.LastName
            };
        }
        public class AlbumIM
        {
            public StatusIM StatusIM { get; set; }

            public Guid AlbumId { get; set; }

            [Required(ErrorMessage = "You must enter an album name")]
            public string AlbumName { get; set; }

            [Range(1900, 2024, ErrorMessage = "You must provide a year between 1900 and 2024")]
            public int ReleaseYear { get; set; }


            [Required(ErrorMessage = "You must enter an album name")]
            public string editAlbumName { get; set; }

            [Range(1900, 2024, ErrorMessage = "You must provide a year between 1900 and 2024")]
            public int editReleaseYear { get; set; }

            public AlbumIM() { }
            public AlbumIM(AlbumIM original)
            {
                StatusIM = original.StatusIM;
                AlbumId = original.AlbumId;
                AlbumName = original.AlbumName;
                ReleaseYear = original.ReleaseYear;


                editAlbumName = original.editAlbumName;
                editReleaseYear = original.editReleaseYear;
            }
            public AlbumIM(IAlbum model)
            {
                StatusIM = StatusIM.Unchanged;
                AlbumId = model.AlbumId;
                AlbumName = editAlbumName = model.Name;
                ReleaseYear = editReleaseYear = model.ReleaseYear;
            }
            
            //to update the model in database
            public IAlbum UpdateModel(IAlbum model)
            {
                model.AlbumId = this.AlbumId;
                model.Name = this.AlbumName;
                model.ReleaseYear = this.ReleaseYear;
                return model;
            }

            //to create new album in the database
            public AlbumCUdto CreateCUdto () => new AlbumCUdto(){

                AlbumId = null,
                Name = this.AlbumName,
                ReleaseYear = this.ReleaseYear
            };
        }
        public class MusicGroupIM
        {
            public StatusIM StatusIM { get; set; }

            public Guid MusicGroupId { get; set; }

            [Required(ErrorMessage = "You must provide a group name")]
            public string Name { get; set; }

            [Range (1900, 2024, ErrorMessage = "You must provide a year between 1900 and 2024")]
            public int EstablishedYear { get; set; }

            //Made nullable and required to force user to make an active selection when creating new group
            [Required(ErrorMessage = "You must select a music genre")]
            public MusicGenre? Genre { get; set; }

            public List<AlbumIM> Albums { get; set; } = new List<AlbumIM>();
            public List<ArtistIM> Artists { get; set; } = new List<ArtistIM>();

            public MusicGroupIM() {}
            public MusicGroupIM(IMusicGroup model)
            {
                StatusIM = StatusIM.Unchanged;
                MusicGroupId = model.MusicGroupId;
                Name = model.Name;
                EstablishedYear = model.EstablishedYear;
                Genre = model.Genre;

                Albums = model.Albums?.Select(m => new AlbumIM(m)).ToList();
                Artists = model.Artists?.Select(m => new ArtistIM(m)).ToList();
            }

            //to update the model in database
            public IMusicGroup UpdateModel(IMusicGroup model)
            {
                model.Name = this.Name;
                model.EstablishedYear = this.EstablishedYear;
                model.Genre = this.Genre.Value;
                return model;
            }

            //to create new music group in the database
            public MusicGroupCUdto CreateCUdto () => new (){
                
                MusicGroupId = null,
                Name = this.Name,
                EstablishedYear = this.EstablishedYear,
                Genre = this.Genre.Value
            };

            //to allow a new album being specified and bound in the form
            public AlbumIM NewAlbum { get; set; } = new AlbumIM();

            //to allow a new album being specified and bound in the form
            public ArtistIM NewArtist { get; set; } = new ArtistIM();         
        }
        #endregion
    }
}
