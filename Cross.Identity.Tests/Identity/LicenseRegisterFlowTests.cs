// namespace Cross.Identity.Tests.Identity;
//
// [Category(TestCategory.UNIT)]
// [TestFixture]
// public class LicenseRegisterFlowTests
// {
//     [Test]
//     public async Task Handle_LicenseRegistrationFlow_FlowNotFound()
//     {
//         // Arrange
//         var command = new RunFlowCommand { FlowId = "non.existent.flow", Input = new Dictionary<string, object>() };
//
//         _flowProviderMock
//             .Setup(x => x.GetFlow(command.FlowId))
//             .ReturnsAsync((Flow)null);
//
//         // Act & Assert
//         await FluentActions.Invoking(() =>
//                 _handler.Handle(command, _cancellationToken))
//             .Should()
//             .ThrowAsync<InvalidOperationException>()
//             .WithMessage($"Flow '{command.FlowId}' not found.");
//     }
//
//     [Test]
//     public async Task CreateUser_ShouldMapDataCorrectly()
//     {
//         // Arrange
//         var formData = new Dictionary<string, object>
//         {
//             ["Email"] = "test@example.com",
//             ["FullName"] = "Test User",
//             ["Company"] = "Test Company",
//             ["AcceptGetEmails"] = true,
//             ["AcceptLicenseTerms"] = true
//         };
//
//         _bag.Set("collectForm", formData);
//
//         var map = new Dictionary<string, string>
//         {
//             ["Email"] = "collectForm.Email",
//             ["FullName"] = "collectForm.FullName",
//             ["Company"] = "collectForm.Company",
//             ["AcceptGetEmails"] = "collectForm.AcceptGetEmails",
//             ["AcceptLicenseTerms"] = "collectForm.AcceptLicenseTerms"
//         };
//
//         _userServiceMock.Setup(x => x.CreateAsync(It.IsAny<UserCreateDto>(), It.IsAny<CancellationToken>()))
//             .ReturnsAsync(new User { Id = "user123", Email = "test@example.com" });
//
//         // Act
//         var result = await CreateUser(_bag, map);
//
//         // Assert
//         result.Should().NotBeNull();
//         _userServiceMock.Verify(
//             x => x.CreateAsync(
//                 It.Is<UserCreateDto>(dto =>
//                     dto.Email == "test@example.com" &&
//                     dto.FullName == "Test User" &&
//                     dto.Company == "Test Company"),
//                 It.IsAny<CancellationToken>()),
//             Times.Once);
//     }
//
//     [Test]
//     public void CollectResult_ShouldCollectAllRequiredData()
//     {
//         // Arrange
//         _bag.Set("sendCode.LastCode", "123456");
//
//         var step = new CollectResultStep
//         {
//             Kind = "collectResult",
//             Map = new Dictionary<string, string> { ["LastCode"] = "sendCode.LastCode" },
//             ResultKey = "Result"
//         };
//
//         // Act
//         var result = step.ExecuteAsync(_bag, CancellationToken.None).Result;
//
//         // Assert
//         result.Should().NotBeNull();
//         var collectedResult = _bag.Get<Dictionary<string, object>>("collectResult.Result");
//         collectedResult.Should().ContainKey("LastCode");
//         collectedResult["LastCode"].Should().Be("123456");
//     }
//
//     [Test]
//     public async Task CollectForm_Should_Validate_Passwords_Equal()
//     {
//         var cfg = BuildConfiguration(new() { ["formInput:ConfirmPassword"] = "DIFF" });
//         await using var sp = BuildServices(cfg);
//         var (runner, bag, _) = BuildFlow(sp, FlowJson);
//
//         Func<Task> act = () => runner.RunAsync("collectForm", bag);
//         await act.Should()
//             .ThrowAsync<InvalidOperationException>()
//             .WithMessage("*Passwords do not match*");
//     }
//
//     [Test]
//     public async Task SendCode_Should_Fail_When_User_NotFound()
//     {
//         var cfg = BuildConfiguration();
//         await using var sp = BuildServices(cfg);
//
//         var sendOnly = """
//                        {
//                          "start": "sendCode",
//                          "steps": [
//                            {
//                              "kind": "sendCode",
//                              "channel": "email",
//                              "selectorKey": "createUser.email",
//                              "resolveBy": { "field": "UserName" },
//                              "next": null
//                            }
//                          ]
//                        }
//                        """;
//
//         var (runner, bag, _) = BuildFlow(sp, sendOnly);
//         bag.Set("createUser.email", "nobody@example.com");
//
//         Func<Task> act = () => runner.RunAsync("sendCode", bag);
//         await act.Should()
//             .ThrowAsync<InvalidOperationException>()
//             .WithMessage("*user not found*");
//     }
// }
