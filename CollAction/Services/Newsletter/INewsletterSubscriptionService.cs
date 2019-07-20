﻿using System.Threading.Tasks;

namespace CollAction.Services.Newsletter
{
    public interface INewsletterSubscriptionService
    {
        Task SetSubscription(string email, bool wantsSubscription, bool requireEmailConfirmationIfSubscribing = true);
        void SetSubscriptionBackground(string email, bool wantsSubscription, bool requireEmailConfirmationIfSubscribing = true);
        Task<bool> IsSubscribedAsync(string email);
    }
}
