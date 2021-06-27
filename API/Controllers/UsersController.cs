using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    public class UsersController : BaseApiController
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly IPhotoService _photoService;

        public UsersController(IUserRepository userRepository, IMapper mapper, IPhotoService photoService)
        {
            _photoService = photoService;
            _mapper = mapper;
            _userRepository = userRepository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MemberDto>>> GetUsers([FromQuery]UserParams userParams)
        {
            var user = await _userRepository.GetUserByUsernameAsync(User.GetUsername());
            userParams.CurrentUsername = user.UserName;

            // filtering to return the results of the opposite gender
            if (string.IsNullOrEmpty(userParams.Gender)) {
                userParams.Gender = user.Gender == "male" ? "female" : "male";
            }

            var users = await _userRepository.GetMembersAsync(userParams);

            Response.AddPaginationHeader(users.CurrentPage, users.PageSize, users.TotalCount, users.TotalPages);

            return Ok(users);
        }

        [HttpGet("{username}", Name="GetUser")]
        public async Task<ActionResult<MemberDto>> GetUser(string username)
        {
            return await _userRepository.GetMemberAsync(username);
        }

        [HttpPut]
        public async Task<ActionResult> UpdateUser(MemberUpdateDto memberUpdateDto)
        {
            // get the username from the token which the api uses to authenticate this user
            var user = await _userRepository.GetUserByUsernameAsync(User.GetUsername());

            // this does all the neccesary mappings i.e., user.City = memberUpdateDto.city;
            _mapper.Map(memberUpdateDto, user);

            _userRepository.Update(user);

            if (await _userRepository.SaveAllAsync()) return NoContent();

            return BadRequest("Failed to update user.");
        }

        [HttpPost("add-photo")]
        public async Task<ActionResult<PhotoDto>> AddPhoto(IFormFile file)
        {
            var user = await _userRepository.GetUserByUsernameAsync(User.GetUsername());
            var result = await _photoService.AddPhotoAsync(file);

            if (result.Error != null) return BadRequest(result.Error.Message);

            var photo = new Photo
            {
                Url = result.SecureUrl.AbsoluteUri,
                PublicId = result.PublicId
            };

            if (user.Photos.Count == 0)
            {
                photo.IsMain = true;
            }

            user.Photos.Add(photo);

            if (await _userRepository.SaveAllAsync()) 
            {
                return CreatedAtRoute("GetUser", new {username = user.UserName}, _mapper.Map<PhotoDto>(photo));
            }

            return BadRequest("Problem adding photo.");
        }

        [HttpPut("set-main-photo/{photoId}")]
        public async Task<ActionResult> SetMainPhoto(int photoId)
        {
            var user = await _userRepository.GetUserByUsernameAsync(User.GetUsername());

            var foundPhoto = user.Photos.FirstOrDefault(photo => photo.Id == photoId);

            if (foundPhoto.IsMain) return BadRequest("This is already your main photo.");

            // find the current main photo and set main to false
            var currentMain = user.Photos.FirstOrDefault(photo => photo.IsMain);
            
            if (currentMain != null) currentMain.IsMain = false;
            
            // update the image of the photoId to be the new image
            foundPhoto.IsMain = true;

            if (await _userRepository.SaveAllAsync()) return NoContent();

            return BadRequest("Failed to set main photo.");
        }

        [HttpDelete("delete-photo/{photoId}")]
        public async Task<ActionResult> DeletePhoto(int photoId)
        {
            var user = await _userRepository.GetUserByUsernameAsync(User.GetUsername());
            var photo = user.Photos.FirstOrDefault(p => p.Id == photoId);

            if (photo == null) return NotFound();

            if (photo.IsMain) return BadRequest("You cannot delete your main photo.");

            // need to remove from Cloudinary as well
            if (photo.PublicId != null)
            {
                var result = await _photoService.DeletePhotoAsync(photo.PublicId);
                if (result.Error != null) return BadRequest(result.Error.Message);
            }

            user.Photos.Remove(photo);

            if (await _userRepository.SaveAllAsync()) return Ok();

            return BadRequest("Failed to delete photo.");
        }
    }
}