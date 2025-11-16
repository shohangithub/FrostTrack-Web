using FluentEmail.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Infrastructure.BackgroundServices
{
    public class EmailBackgroundService(
    IServiceScopeFactory serviceScopeFactory,
    IDateTimeProvider _dateTimeProvider,
    IFluentEmail _fluentEmail) : IHostedService, IDisposable
    {
       // private readonly ApplicationDbContext _dbContext = serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredService<ApplicationDbContext>();
        private Timer _timer = null!;

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(SendEmailNotifications, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        /// <summary>
        /// TODO: there are many edge cases that aren't caught here. This is an immediate nice to have implementation for now.
        /// </summary>
        private async void SendEmailNotifications(object? state)
        {
            var now = _dateTimeProvider.UtcNow;
            var oneMinuteFromNow = now.AddMinutes(1);

            //var users = _dbContext.AppUsers
            //    //.Where(reminder => reminder.DateTime >= now && reminder.DateTime <= oneMinuteFromNow && !reminder.IsDismissed)
            //    //.GroupBy(reminder => reminder.SubscriptionId)
            //    .ToList();

            ////var subscriptionToBeNotified = users.ConvertAll(x => x.Email);

            ////var usersToBeNotified = _dbContext.Users
            ////    .Where(user => subscriptionToBeNotified.Contains(user.Id))
            ////    .ToList();

            //foreach (User? user in users)
            //{
            //    //var dueReminders = dueRemindersBySubscription
            //    //    .Single(x => x.Key == user.Subscription.Id)
            //    //    .ToList();

            //    await _fluentEmail
            //        .To(user.Email)
            //        .Subject($"Greetings ")
            //        .Body($"""
            //          Dear {user.UserName} from the present.

            //          I hope this email finds you well.=
            //          Best,
            //          Rafiul Islam
            //          """)
            //        .SendAsync();
            //}
        }
    }
}
