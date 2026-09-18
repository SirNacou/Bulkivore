using Bulkivore.Api.Domain.Imports.Ports;
using Bulkivore.Api.Domain.Schema;
using Bulkivore.Api.Infrastructure.Services;

namespace Bulkivore.UnitTests;

public class FuzzyColumnMatcherTests
{
    private readonly FuzzyColumnMatcher _matcher = new();

    [Test]
    public async Task Match_WeakEarlierHeader_DoesNotStealTargetFromStrongerLaterHeader()
    {
        // "shipping date" scores 81.8 against ship_date, "shipdate" scores 94.1.
        var matches = Match(["shipping date", "shipdate"], ["ship_date", "delivery_date"]);

        await Assert.That(matches[0].TargetColumn).IsEqualTo(string.Empty);
        await Assert.That(matches[0].IsAutoMatched).IsFalse();
        await Assert.That(matches[1].TargetColumn).IsEqualTo("ship_date");
        await Assert.That(matches[1].Confidence).IsEqualTo(0.94);
    }

    [Test]
    public async Task Match_WeakFuzzyHeader_LosesExactNameToLaterHeader()
    {
        // "status" scores 90 against order_status but is not exact;
        // "order status" would have been left unmatched by the old greedy loop.
        var matches = Match(["status", "order status", "order date"], ["order_status", "order_date", "order_id"]);

        await Assert.That(matches[0].TargetColumn).IsEqualTo(string.Empty);
        await Assert.That(matches[1].TargetColumn).IsEqualTo("order_status");
        await Assert.That(matches[1].Confidence).IsEqualTo(1.0);
        await Assert.That(matches[2].TargetColumn).IsEqualTo("order_date");
    }

    [Test]
    public async Task Match_ExactNameWins_EvenWhenAnotherHeaderScoredHigher()
    {
        // "product name v2" scores 95 against "product name", but the exact
        // normalized matches must claim that target first.
        var matches = Match(
            ["product_name", "PRODUCT NAME", "product name v2"],
            ["product name", "product_label"]);

        await Assert.That(matches[0].TargetColumn).IsEqualTo("product name");
        await Assert.That(matches[0].Confidence).IsEqualTo(1.0);
        await Assert.That(matches[1].TargetColumn).IsEqualTo("product_label");
        await Assert.That(matches[2].TargetColumn).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Match_EqualScores_AssignsEarlierHeaderFirst()
    {
        // Both headers score exactly 90 against "price"; the earlier header wins
        // deterministically and the later one finds nothing else above the cutoff.
        var matches = Match(["price usd", "unitprice"], ["price", "currency"]);

        await Assert.That(matches[0].TargetColumn).IsEqualTo("price");
        await Assert.That(matches[0].Confidence).IsEqualTo(0.9);
        await Assert.That(matches[1].TargetColumn).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Match_BelowDefaultThreshold_DoesNotAutoMatch()
    {
        // "qty" scores 72 against "quantity"; the old code cast the threshold to a
        // cutoff of 0 and accepted it anyway.
        var matches = Match(["qty"], ["quantity"]);

        await Assert.That(matches[0].TargetColumn).IsEqualTo(string.Empty);
        await Assert.That(matches[0].IsAutoMatched).IsFalse();
    }

    [Test]
    public async Task Match_CustomThreshold_MatchesSubEightyScores()
    {
        var matches = Match(["qty"], ["quantity"], confidenceThreshold: 0.7);

        await Assert.That(matches[0].TargetColumn).IsEqualTo("quantity");
        await Assert.That(matches[0].Confidence).IsEqualTo(0.72);
        await Assert.That(matches[0].IsAutoMatched).IsTrue();
    }

    [Test]
    public async Task Match_SeveralCompetingHeaders_AssignsEachTargetAtMostOnce()
    {
        // All three headers score ~90 against both targets; scores must be
        // distributed one-to-one with the leftovers unmatched.
        var matches = Match(["e-mail", "email_address", "customer_email"], ["email", "mail"]);

        await Assert.That(matches.Count).IsEqualTo(3);
        await Assert.That(matches[0].TargetColumn).IsEqualTo("email");
        await Assert.That(matches[1].TargetColumn).IsEqualTo("mail");
        await Assert.That(matches[2].TargetColumn).IsEqualTo(string.Empty);

        var assigned = matches.Where(m => m.TargetColumn.Length > 0).Select(m => m.TargetColumn).ToList();
        await Assert.That(assigned.Count).IsEqualTo(assigned.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Test]
    public async Task Match_EmptyInputs_ReturnsNoMatches()
    {
        var noHeaders = Match([], ["email"]);
        var noTargets = Match(["email"], []);

        await Assert.That(noHeaders).IsEmpty();
        await Assert.That(noTargets).IsEmpty();
    }

    [Test]
    public async Task Match_BlankHeaders_AreSkipped()
    {
        var matches = Match(["", "   ", "product name"], ["product name"]);

        await Assert.That(matches.Count).IsEqualTo(1);
        await Assert.That(matches[0].SourceHeader).IsEqualTo("product name");
        await Assert.That(matches[0].TargetColumn).IsEqualTo("product name");
    }

    [Test]
    public async Task Match_ResultOrder_FollowsHeaderOrder()
    {
        var matches = Match(["order date", "status", "order status"], ["order_status", "order_date", "order_id"]);

        await Assert.That(matches.Select(m => m.SourceHeader).ToArray())
            .IsEquivalentTo(["order date", "status", "order status"]);
        await Assert.That(matches[0].TargetColumn).IsEqualTo("order_date");
        await Assert.That(matches[2].TargetColumn).IsEqualTo("order_status");
    }

    private IReadOnlyList<ColumnMatch> Match(
        string[] headers,
        string[] targets,
        double confidenceThreshold = 0.8)
    {
        var targetColumns = targets
            .Select(name => new ColumnMetadata(name, ColumnDataType.Text, IsNullable: true, MaxLength: null, IsIdentity: false, HasDefault: false, IsGenerated: false))
            .ToList();

        return _matcher.Match(headers, targetColumns, confidenceThreshold);
    }
}
