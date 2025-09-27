// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using qenem.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using System.Linq;

namespace qenem.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
        }

        public string Username { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        // Lista de roles do usuário (exibimos apenas; não modificamos nada aqui)
        public IList<string> Roles { get; set; }

        public bool IsEmailConfirmed { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            [Display(Name = "Username")]
            public string Username { get; set; }

            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Phone]
            [Display(Name = "Phone number")]
            public string PhoneNumber { get; set; }
        }

        private async Task LoadAsync(ApplicationUser user)
        {
            Username = await _userManager.GetUserNameAsync(user);
            var email = await _userManager.GetEmailAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);

            // recupera roles do usuário (não altera nada — apenas exibe)
            Roles = (await _userManager.GetRolesAsync(user)) ?? new List<string>();

            IsEmailConfirmed = await _userManager.IsEmailConfirmedAsync(user);

            Input = new InputModel
            {
                Username = Username,
                Email = email,
                PhoneNumber = phoneNumber,
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Não foi possível carregar o usuário com ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        // Atualiza perfil (username, email, phone, campos customizados)
        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Não foi possível carregar o usuário com ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            // USERNAME
            var currentUserName = await _userManager.GetUserNameAsync(user);
            if (Input.Username != currentUserName)
            {
                var setUserNameResult = await _userManager.SetUserNameAsync(user, Input.Username);
                if (!setUserNameResult.Succeeded)
                {
                    foreach (var e in setUserNameResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, e.Description);
                    }
                    await LoadAsync(user);
                    return Page();
                }
            }

            // EMAIL
            var currentEmail = await _userManager.GetEmailAsync(user);
            if (Input.Email != currentEmail)
            {
                var setEmailResult = await _userManager.SetEmailAsync(user, Input.Email);
                if (!setEmailResult.Succeeded)
                {
                    foreach (var e in setEmailResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, e.Description);
                    }
                    await LoadAsync(user);
                    return Page();
                }
            }

            // PHONE
            var currentPhone = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != currentPhone)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    ModelState.AddModelError(string.Empty, "Erro ao atualizar o telefone.");
                    await LoadAsync(user);
                    return Page();
                }
            }

            // CAMPOS CUSTOMIZADOS: FirstName / LastName (se existirem na sua ApplicationUser)
            var changed = false;

            if (changed)
            {
                var updateResult = await _userManager.UpdateAsync(user);
                if (!updateResult.Succeeded)
                {
                    foreach (var e in updateResult.Errors)
                        ModelState.AddModelError(string.Empty, e.Description);

                    await LoadAsync(user);
                    return Page();
                }
            }

            // Não alteramos roles aqui (preservando alterações relacionadas a roles).
            // Se houver lógica custom de roles, ela fica fora desta página.

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Seu perfil foi atualizado.";
            return RedirectToPage();
        }

        // Handler para envio de email de confirmação
        public async Task<IActionResult> OnPostSendVerificationEmailAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Não foi possível carregar o usuário com ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { area = "Identity", userId = user.Id, code },
                protocol: Request.Scheme);

            await _emailSender.SendEmailAsync(
                Input.Email,
                "Confirm your email",
                $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

            StatusMessage = "Email de confirmação enviado. Verifique sua caixa de entrada.";
            return RedirectToPage();
        }
    }
}