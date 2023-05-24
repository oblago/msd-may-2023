﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using JobApplicationMvc.Areas.Identity.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using DotNetCore.CAP;
using DomainEvents = MessageContracts.WebMessages;

namespace JobApplicationMvc.Areas.Identity.Pages.Account
{
    public class ConfirmEmailModel : PageModel
    {
        private readonly UserManager<JobApplicationMvcUser> _userManager;
        private readonly ICapPublisher _publisher;

        public ConfirmEmailModel(UserManager<JobApplicationMvcUser> userManager, ICapPublisher publisher)
        {
            _userManager = userManager;
            _publisher = publisher;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }
        public async Task<IActionResult> OnGetAsync(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return RedirectToPage("/Index");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userId}'.");
            }

            code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            var result = await _userManager.ConfirmEmailAsync(user, code);

            // TODO: Add our code here to produce domain event.
            StatusMessage = result.Succeeded ? "Thank you for confirming your email." : "Error confirming your email.";

            var domainEvent = new DomainEvents.ApplicantCreated
            {
                DateOfBirth = user.DateOfBirth,
                EmailAddress = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                UserId = user.Id,

            };

            await _publisher.PublishAsync(DomainEvents.ApplicantCreated.MessageId, domainEvent);
            //use PublishDelay() if you need to save data firts
            return Page();
        }
    }
}
