﻿using System;
using System.Threading.Tasks;
using Bit.Api.Models.Request.Organizations;
using Bit.Core.Context;
using Bit.Core.Entities;
using Bit.Core.Exceptions;
using Bit.Core.OrganizationFeatures.OrganizationSponsorships.FamiliesForEnterprise.Interfaces;
using Bit.Core.Repositories;
using Bit.Core.Services;
using Bit.Core.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bit.Api.Controllers
{
    [Route("organization/sponsorship")]
    [Authorize("Application")]
    public class OrganizationSponsorshipsController : Controller
    {
        private readonly IOrganizationSponsorshipRepository _organizationSponsorshipRepository;
        private readonly IOrganizationRepository _organizationRepository;
        private readonly IOrganizationUserRepository _organizationUserRepository;
        private readonly IValidateRedemptionTokenCommand _validateRedemptionTokenCommand;
        private readonly IOfferSponsorshipCommand _offerSponsorshipCommand;
        private readonly IResendSponsorshipOfferCommand _resendSponsorshipOfferCommand;
        private readonly ISetUpSponsorshipCommand _setUpSponsorshipCommand;
        private readonly IRevokeSponsorshipCommand _revokeSponsorshipCommand;
        private readonly IRemoveSponsorshipCommand _removeSponsorshipCommand;
        private readonly ICurrentContext _currentContext;
        private readonly IUserService _userService;

        public OrganizationSponsorshipsController(
            IOrganizationSponsorshipRepository organizationSponsorshipRepository,
            IOrganizationRepository organizationRepository,
            IOrganizationUserRepository organizationUserRepository,
            IValidateRedemptionTokenCommand validateRedemptionTokenCommand,
            IOfferSponsorshipCommand offerSponsorshipCommand,
            IResendSponsorshipOfferCommand resendSponsorshipOfferCommand,
            ISetUpSponsorshipCommand setUpSponsorshipCommand,
            IRevokeSponsorshipCommand revokeSponsorshipCommand,
            IRemoveSponsorshipCommand removeSponsorshipCommand,
            IUserService userService,
            ICurrentContext currentContext)
        {
            _organizationSponsorshipRepository = organizationSponsorshipRepository;
            _organizationRepository = organizationRepository;
            _organizationUserRepository = organizationUserRepository;
            _validateRedemptionTokenCommand = validateRedemptionTokenCommand;
            _offerSponsorshipCommand = offerSponsorshipCommand;
            _resendSponsorshipOfferCommand = resendSponsorshipOfferCommand;
            _setUpSponsorshipCommand = setUpSponsorshipCommand;
            _revokeSponsorshipCommand = revokeSponsorshipCommand;
            _removeSponsorshipCommand = removeSponsorshipCommand;
            _userService = userService;
            _currentContext = currentContext;
        }

        [HttpPost("{sponsoringOrgId}/families-for-enterprise")]
        [SelfHosted(NotSelfHostedOnly = true)]
        public async Task CreateSponsorship(Guid sponsoringOrgId, [FromBody] OrganizationSponsorshipRequestModel model)
        {
            await _offerSponsorshipCommand.OfferSponsorshipAsync(
                await _organizationRepository.GetByIdAsync(sponsoringOrgId),
                await _organizationUserRepository.GetByOrganizationAsync(sponsoringOrgId, _currentContext.UserId ?? default),
                model.PlanSponsorshipType, model.SponsoredEmail, model.FriendlyName,
                (await CurrentUser).Email);
        }

        [HttpPost("{sponsoringOrgId}/families-for-enterprise/resend")]
        [SelfHosted(NotSelfHostedOnly = true)]
        public async Task ResendSponsorshipOffer(Guid sponsoringOrgId)
        {
            var sponsoringOrgUser = await _organizationUserRepository
                .GetByOrganizationAsync(sponsoringOrgId, _currentContext.UserId ?? default);

            await _resendSponsorshipOfferCommand.ResendSponsorshipOfferAsync(
                await _organizationRepository.GetByIdAsync(sponsoringOrgId),
                sponsoringOrgUser,
                await _organizationSponsorshipRepository
                    .GetBySponsoringOrganizationUserIdAsync(sponsoringOrgUser.Id),
                (await CurrentUser).Email);
        }

        [HttpPost("validate-token")]
        [SelfHosted(NotSelfHostedOnly = true)]
        public async Task<bool> PreValidateSponsorshipToken([FromQuery] string sponsorshipToken)
        {
            return await _validateRedemptionTokenCommand.ValidateRedemptionTokenAsync(sponsorshipToken, (await CurrentUser).Email);
        }

        [HttpPost("redeem")]
        [SelfHosted(NotSelfHostedOnly = true)]
        public async Task RedeemSponsorship([FromQuery] string sponsorshipToken, [FromBody] OrganizationSponsorshipRedeemRequestModel model)
        {
            if (!await _validateRedemptionTokenCommand.ValidateRedemptionTokenAsync(sponsorshipToken, (await CurrentUser).Email))
            {
                throw new BadRequestException("Failed to parse sponsorship token.");
            }

            if (!await _currentContext.OrganizationOwner(model.SponsoredOrganizationId))
            {
                throw new BadRequestException("Can only redeem sponsorship for an organization you own.");
            }

            await _setUpSponsorshipCommand.SetUpSponsorshipAsync(
                await _organizationSponsorshipRepository
                    .GetByOfferedToEmailAsync((await CurrentUser).Email),
                await _organizationRepository.GetByIdAsync(model.SponsoredOrganizationId));
        }

        [HttpDelete("{sponsoringOrganizationId}")]
        [HttpPost("{sponsoringOrganizationId}/delete")]
        [SelfHosted(NotSelfHostedOnly = true)]
        public async Task RevokeSponsorship(Guid sponsoringOrganizationId)
        {

            var orgUser = await _organizationUserRepository.GetByOrganizationAsync(sponsoringOrganizationId, _currentContext.UserId ?? default);
            if (_currentContext.UserId != orgUser?.UserId)
            {
                throw new BadRequestException("Can only revoke a sponsorship you granted.");
            }

            var existingOrgSponsorship = await _organizationSponsorshipRepository
                .GetBySponsoringOrganizationUserIdAsync(orgUser.Id);

            await _revokeSponsorshipCommand.RevokeSponsorshipAsync(
                await _organizationRepository
                    .GetByIdAsync(existingOrgSponsorship.SponsoredOrganizationId ?? default),
                existingOrgSponsorship);
        }

        [HttpDelete("sponsored/{sponsoredOrgId}")]
        [HttpPost("sponsored/{sponsoredOrgId}/remove")]
        [SelfHosted(NotSelfHostedOnly = true)]
        public async Task RemoveSponsorship(Guid sponsoredOrgId)
        {

            if (!await _currentContext.OrganizationOwner(sponsoredOrgId))
            {
                throw new BadRequestException("Only the owner of an organization can remove sponsorship.");
            }

            var existingOrgSponsorship = await _organizationSponsorshipRepository
                .GetBySponsoredOrganizationIdAsync(sponsoredOrgId);

            await _removeSponsorshipCommand.RemoveSponsorshipAsync(
                await _organizationRepository
                    .GetByIdAsync(existingOrgSponsorship.SponsoredOrganizationId.Value),
                existingOrgSponsorship);
        }

        private Task<User> CurrentUser => _userService.GetUserByIdAsync(_currentContext.UserId.Value);
    }
}
