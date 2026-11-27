using Cenvo.Modules.Cafe.Domain;
using Cenvo.Modules.Cafe.Domain.Calculations;

namespace Cenvo.Tests.Unit;

public class KitchenFlowTests
{
    [Theory]
    [InlineData(KitchenStatus.Sent, KitchenStatus.Preparing)]
    [InlineData(KitchenStatus.Sent, KitchenStatus.Done)]      // skipping a step is fine
    [InlineData(KitchenStatus.Done, KitchenStatus.Served)]
    [InlineData(KitchenStatus.Done, KitchenStatus.Preparing)] // undo one mistaken tap
    [InlineData(KitchenStatus.Done, KitchenStatus.Done)]      // two screens tapping at once
    public void Allowed_moves(KitchenStatus from, KitchenStatus to) => Assert.True(KitchenFlow.CanMove(from, to));

    [Theory]
    [InlineData(KitchenStatus.Served, KitchenStatus.Preparing)] // back two steps
    [InlineData(KitchenStatus.Sent, KitchenStatus.New)]         // only "send" changes New
    [InlineData(KitchenStatus.New, KitchenStatus.Preparing)]    // not sent yet
    public void Forbidden_moves(KitchenStatus from, KitchenStatus to) => Assert.False(KitchenFlow.CanMove(from, to));
}
