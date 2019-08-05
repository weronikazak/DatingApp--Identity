using AutoMapper;
using CloudinaryDotNet;
using DatingApp.API.Data;
using DatingApp.API.Helpers;
using DatingApp.API.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using System.Security.Claims;
using CloudinaryDotNet.Actions;
using DatingApp.API.Models;
using System.Linq;

namespace DatingApp.API.Controllers
{
    [Authorize]
    [Route("api/user/{userId}/photos")]
    [ApiController]
    public class PhotosController : ControllerBase
    {
        private IOptions<CloudinarySettings> _cloudinaryConfig;
        private IMapper _mapper;
        private IDatingRepository _repo;
        private Cloudinary _cloudinary;

        public PhotosController(IDatingRepository repo, IMapper mapper, IOptions<CloudinarySettings> cloudinaryConfig)
        {
            _cloudinaryConfig = cloudinaryConfig;
            _mapper = mapper;
            _repo = repo;

            Account acc = new Account(
                _cloudinaryConfig.Value.CloudName,
                _cloudinaryConfig.Value.ApiKey,
                _cloudinaryConfig.Value.ApiSecret
            );

            _cloudinary = new Cloudinary(acc);
        }
        
        [HttpGet("{id}", Name = "GetPhoto")]
        public async Task<IActionResult> GetPhoto(int id)
        {
            var photoFromRepo = await _repo.GetPhoto(id);
            var photo = _mapper.Map<PhotoForReturnDto>(photoFromRepo);
 
            return Ok(photo);
        }
        
        [HttpPost]
        public async Task<IActionResult> AddPhotoForUser(int userId, [FromForm]PhotoForCreationDto photoForCreationDto)
        {
            if (userId != int.Parse (User.FindFirst (ClaimTypes.NameIdentifier).Value)) {
                return Unauthorized ();
            }
 
            var userFromRepo = await _repo.GetUser(userId);
 
            var file = photoForCreationDto.File;
            var uploadresults = new ImageUploadResult();
 
            if (file.Length > 0)
            {
                using (var stream = file.OpenReadStream())
                {
                    var uploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(file.Name, stream),
                        Transformation = new Transformation()
                            .Width(500).Height(500).Crop("fill").Gravity("face")
                    };
 
                    uploadresults = _cloudinary.Upload(uploadParams);
                }
            }
 
            photoForCreationDto.Url = uploadresults.Uri.ToString();
            photoForCreationDto.PublicId = uploadresults.PublicId;
 
            var photo = _mapper.Map<Photo>(photoForCreationDto);
 
            if (!userFromRepo.Photos.Any(u => u.IsMain))
            {
                photo.IsMain = true;
            }
 
            
            userFromRepo.Photos.Add(photo);
            if (await _repo.SaveAll())
            {
                var photoToReturn = _mapper.Map<PhotoForReturnDto>(photo);
                return CreatedAtRoute("GetPhoto", new {id = photo.Id}, photoToReturn);
            }
 
            return BadRequest("could not upload photo");
        }

        [HttpPost("{photoId}/setMain")]
        public async Task<IActionResult> SetMainPhoto(int userId, int photoId){
            if (userId != int.Parse (User.FindFirst (ClaimTypes.NameIdentifier).Value)) {
                return Unauthorized();
            }

            var user = await _repo.GetUser(userId);

            if (!user.Photos.Any(p => p.Id == photoId)){
                return Unauthorized();
            }
            

            var photoFromRepo = await _repo.GetPhoto(photoId);

            if (photoFromRepo.IsMain){
                return BadRequest("The selected photo already is the main photo");
            }

            var currentMainPhoto = await _repo.GetMainPhotoForUser(userId);

            currentMainPhoto.IsMain = false;

            photoFromRepo.IsMain = true;

            if (await _repo.SaveAll())
                return NoContent();

            return BadRequest("Unable to set photo as main");

        }

        [HttpDelete("{photoId}")]
        public async Task<IActionResult> DeletePhoto(int userId, int photoId )
        {
            if (userId != int.Parse (User.FindFirst (ClaimTypes.NameIdentifier).Value)) {
                return Unauthorized();
            }

            var user = await _repo.GetUser(userId);

            if (!user.Photos.Any(p => p.Id == photoId)){
                return Unauthorized();
            }
            

            var photoFromRepo = await _repo.GetPhoto(photoId);

            if (photoFromRepo.IsMain){
                return BadRequest("You cannot delete your main photo");
            }

            if (photoFromRepo.PublicID != null){
                var deleteParms = new DeletionParams(photoFromRepo.PublicID);

                var result = _cloudinary.Destroy(deleteParms);

                if (result.Result == "ok"){
                    _repo.Delete(photoFromRepo);
                }
            }

            if (photoFromRepo.PublicID == null){
                _repo.Delete(photoFromRepo);
            }

            
            if (await _repo.SaveAll())
                return Ok();

            return BadRequest("Failed to delete the phone");
            
        }
    }
}