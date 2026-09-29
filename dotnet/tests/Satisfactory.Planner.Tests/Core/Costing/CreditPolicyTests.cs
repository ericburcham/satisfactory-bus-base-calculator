using Satisfactory.Planner.Core.Costing;
using Satisfactory.Planner.Core.Identifiers;

namespace Satisfactory.Planner.Tests.Core.Costing;

[TestFixture]
public sealed class CreditPolicyTests
{
    private static readonly ItemId Residue = new("Desc_HeavyOilResidue_C");
    private static readonly ItemId Coke = new("Desc_PetroleumCoke_C");

    [Test]
    public void NewPolicy_CreditsEverything_ForbidsNothing()
    {
        var credit = new CreditPolicy();

        Assert.Multiple(() =>
        {
            Assert.That(credit.NoCredit, Is.Empty);
            Assert.That(credit.NoByproduct, Is.Empty);
            Assert.That(credit.EarnsCredit(Coke), Is.True);
            Assert.That(credit.IsForbiddenByproduct(Residue), Is.False);
        });
    }

    [Test]
    public void DenyCredit_AddsToNoCreditOnly()
    {
        var credit = new CreditPolicy();

        credit.DenyCredit([Coke, Residue]);
        credit.DenyCredit([Coke]);

        Assert.Multiple(() =>
        {
            Assert.That(credit.NoCredit, Is.EquivalentTo(new[] { Coke, Residue }));
            Assert.That(credit.EarnsCredit(Coke), Is.False);
            Assert.That(credit.NoByproduct, Is.Empty);
        });
    }

    [Test]
    public void ForbidByproduct_AddsToNoByproductOnly()
    {
        var credit = new CreditPolicy();

        credit.ForbidByproduct(Residue);

        Assert.Multiple(() =>
        {
            Assert.That(credit.IsForbiddenByproduct(Residue), Is.True);
            Assert.That(credit.IsForbiddenByproduct(Coke), Is.False);
            Assert.That(credit.EarnsCredit(Residue), Is.True);
        });
    }

    [Test]
    public void DenyCredit_Null_Throws() =>
        Assert.That(() => new CreditPolicy().DenyCredit(null!), Throws.ArgumentNullException);
}
