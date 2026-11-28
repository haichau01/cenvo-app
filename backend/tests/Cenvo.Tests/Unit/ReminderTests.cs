using Cenvo.Modules.CarWash.Domain;
using Cenvo.Modules.CarWash.Domain.Calculations;
using Cenvo.SharedKernel;

namespace Cenvo.Tests.Unit;

public class ReminderTemplateTests
{
    [Fact]
    public void Render_fills_known_placeholders_and_blanks_missing_values()
    {
        var text = ReminderTemplate.Render("Chào {ten}, xe {bienso} — {goi}.", new Dictionary<string, string?>
        {
            ["ten"] = "Chị Lan", ["bienso"] = "51A12345",
        });
        Assert.Equal("Chào Chị Lan, xe 51A12345 — .", text);
    }

    [Fact]
    public void Render_keeps_unknown_placeholders_as_typed()
    {
        Assert.Equal("Chào {tenkhach}", ReminderTemplate.Render("Chào {tenkhach}", new Dictionary<string, string?>()));
    }

    [Fact]
    public void Render_cuts_an_overlong_message()
    {
        var text = ReminderTemplate.Render("{ten}", new Dictionary<string, string?> { ["ten"] = new string('a', 1000) });
        Assert.Equal(ReminderTemplate.MaxMessageLength, text.Length);
    }

    [Fact]
    public void UnknownPlaceholders_lists_typos_once()
    {
        Assert.Equal(["tenkhach"], ReminderTemplate.UnknownPlaceholders("{ten} {tenkhach} {tenkhach} {hethan}"));
        Assert.Empty(ReminderTemplate.UnknownPlaceholders(ReminderSettings.DefaultReturnTemplate));
        Assert.Empty(ReminderTemplate.UnknownPlaceholders(ReminderSettings.DefaultExpiringTemplate));
        Assert.Empty(ReminderTemplate.UnknownPlaceholders(ReminderSettings.DefaultLowUsesTemplate));
    }
}

public class ReminderSettingsTests
{
    private static void Update(ReminderSettings s, int days = 14, int lowUses = 1, string template = "Chào {ten}") =>
        s.Update(days, 7, lowUses, template, template, template);

    [Theory]
    [InlineData(0)]
    [InlineData(366)]
    public void Days_out_of_range_are_rejected(int days) =>
        Assert.Throws<BusinessException>(() => Update(new ReminderSettings(), days: days));

    [Fact]
    public void Empty_overlong_or_misspelled_templates_are_rejected()
    {
        Assert.Throws<BusinessException>(() => Update(new ReminderSettings(), template: "  "));
        Assert.Throws<BusinessException>(() => Update(new ReminderSettings(), template: new string('a', 321)));
        Assert.Throws<BusinessException>(() => Update(new ReminderSettings(), template: "Chào {tenkhach}"));
        Assert.Throws<BusinessException>(() => Update(new ReminderSettings(), lowUses: 11));
    }
}

public class CustomerReminderTests
{
    private static readonly DateTime Now = new(2026, 11, 28, 2, 0, 0, DateTimeKind.Utc);

    private static CustomerReminder Claimed() =>
        CustomerReminder.Claim(ReminderKind.ReturnVisit, Guid.NewGuid(), Guid.NewGuid(), "0901234567", "Chào", null, Now);

    [Fact]
    public void A_sent_reminder_can_never_be_sent_again()
    {
        var reminder = Claimed();
        reminder.MarkSent("p-1", Now);
        Assert.False(reminder.CanSend(Now.AddDays(30)));
        Assert.Throws<BusinessException>(() => reminder.Retry("0901234567", "Chào", null, Now.AddDays(30)));
    }

    [Fact]
    public void A_failed_reminder_can_be_retried_and_counts_attempts()
    {
        var reminder = Claimed();
        reminder.MarkFailed("Sai số");
        Assert.True(reminder.CanSend(Now));
        reminder.Retry("0907654321", "Chào lại", null, Now.AddMinutes(1));
        Assert.Equal(2, reminder.Attempts);
        Assert.Equal(ReminderStatus.Pending, reminder.Status);
        Assert.Equal("0907654321", reminder.Phone);
    }

    [Fact]
    public void A_pending_send_blocks_until_it_times_out()
    {
        var reminder = Claimed();
        Assert.False(reminder.CanSend(Now.AddMinutes(5)));
        Assert.True(reminder.CanSend(Now + CustomerReminder.PendingTimeout + TimeSpan.FromSeconds(1)));
    }

    [Fact]
    public void Failure_without_a_reason_gets_a_readable_message()
    {
        var reminder = Claimed();
        reminder.MarkFailed(null);
        Assert.Equal(CarWashErrors.ReminderSendFailed, reminder.LastError);
    }
}

public class CustomerLastWashTests
{
    [Fact]
    public void An_older_wash_does_not_replace_the_latest_visit()
    {
        var customer = Customer.Create("0901234567", "Lan");
        var latest = Guid.NewGuid();
        var at = new DateTime(2026, 11, 20, 0, 0, 0, DateTimeKind.Utc);
        customer.RecordWash(latest, Guid.NewGuid(), at);
        customer.RecordWash(Guid.NewGuid(), Guid.NewGuid(), at.AddDays(-3));
        Assert.Equal(latest, customer.LastTicketId);
        Assert.Equal(at, customer.LastWashAt);
    }
}

public class PlateFormatTests
{
    [Theory]
    [InlineData("51A12345", "51A-123.45")]
    [InlineData("30K1234", "30K-1234")]
    [InlineData("51LD12345", "51LD-123.45")]
    [InlineData("ABC12", "ABC12")]
    public void Format_matches_the_frontend_rule(string stored, string shown) => Assert.Equal(shown, PlateNumber.Format(stored));
}
