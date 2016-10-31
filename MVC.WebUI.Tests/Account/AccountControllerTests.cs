﻿namespace MVC.WebUI.Tests.Account
{
    using System.Web;
    using System.Web.Mvc;
    using System.Web.Routing;
    using Controllers;
    using Core.Exceptions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Models.Account;
    using Moq;
    using Services.Account;
    using Services.Account.Transfer;
    using Services.Message;

    // http://www.asp.net/mvc/overview/older-versions-1/unit-testing/creating-unit-tests-for-asp-net-mvc-applications-cs

    [TestClass]
    public class AccountControllerTests
    {
        private Mock<HttpContextBase> mockContext;

        [TestInitialize]
        public void Init()
        {
            this.mockContext = new Mock<HttpContextBase>();
        }

        [TestMethod]
        public void Create()
        {
            // arrange
            var mockAuthenticationService = new Mock<IAuthenticationService>();
            var controller = new AccountController(mockAuthenticationService.Object, null, null);

            // act
            var result = controller.Create() as ViewResult;

            // assert
            Assert.IsNotNull(result);
            Assert.AreEqual(string.Empty, result.ViewName);
        }

        [TestMethod]
        public void Create_Redirect()
        {
            // arrange
            var mockAuthenticationService = new Mock<IAuthenticationService>();
            mockAuthenticationService.SetupGet(m => m.IsAuthenticated).Returns(true);
            var controller = new AccountController(mockAuthenticationService.Object, null, null);

            // act
            var result = controller.Create() as RedirectToRouteResult;

            // assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.RouteValues["action"]);
            Assert.AreEqual("Home", result.RouteValues["controller"]);
        }

        [TestMethod]
        public void Create_Post_OK()
        {
            // arrange
            var mockAuthenticationService = new Mock<IAuthenticationService>();

            var mockAccountService = new Mock<IAccountService>();
            mockAccountService.Setup(m => m.CreateAccount(It.IsAny<CreateAccountRequest>()))
                .Returns(new CreateAccountResponse
                {
                    Status = StatusCode.OK,
                    ActivateAccountToken = "YYYYYYYYYYYY",
                    CreateAccountStatus = CreateAccountStatus.Success
                });

            var mockMessagingService = new Mock<IMessageService>();
            var controller = new AccountController(mockAuthenticationService.Object, mockAccountService.Object, mockMessagingService.Object);


            //controller.Url = new UrlHelper(
            //    new RequestContext(this.mockContext.Object, new RouteData()),
            //    new RouteCollection()
            //);


            // mock url needed to mock the call to Url.Action
            //var mockUrl = new Mock<UrlHelper>();
            //mockUrl.Setup(x => x.Action("ActivateAccount", "Email", new { name = "FirstName", token = "token" })).Returns("/email/activateaccount?name=XXX&token=YYY");
            //controller.Url = mockUrl.Object;


            var model = new CreateAccountViewModel
            {
                Email = "email@email.com",
                Password = "XXXXXXXX",
                ConfirmPassword = "XXXXXXXX"
            };

            // act
            var result = controller.Create(model) as RedirectToRouteResult;

            // assert
            Assert.IsNotNull(result);
            Assert.AreEqual("LogIn", result.RouteValues["action"]);
            Assert.AreEqual(null, result.RouteValues["controller"]);
            Assert.IsNotNull(controller.TempData["SuccessMessage"]);
        }

        [TestMethod]
        public void Create_Post_BadRequest()
        {
            // arrange
            var mockAuthenticationService = new Mock<IAuthenticationService>();

            var mockAccountService = new Mock<IAccountService>();
            mockAccountService.Setup(m => m.CreateAccount(It.IsAny<CreateAccountRequest>()))
                .Returns(new CreateAccountResponse
                {
                    Status = StatusCode.BadRequest
                });

            var controller = new AccountController(mockAuthenticationService.Object, mockAccountService.Object, null);

            var model = new CreateAccountViewModel
            {
                Email = "email@email.com",
                Password = "XXXXXXXX",
                ConfirmPassword = "XXXXXXXX"
            };

            // act
            var result = controller.Create(model) as ViewResult;

            // assert
            Assert.IsNotNull(result);
            Assert.AreEqual(string.Empty, result.ViewName);
            Assert.IsFalse(result.ViewData.ModelState.IsValid);
            Assert.AreEqual(1, result.ViewData.ModelState.Count);
        }

        [TestMethod]
        public void Create_Post_InternalServerErrror()
        {
            // arrange
            var mockAuthenticationService = new Mock<IAuthenticationService>();

            var mockAccountService = new Mock<IAccountService>();
            mockAccountService.Setup(m => m.CreateAccount(It.IsAny<CreateAccountRequest>()))
                .Returns(new CreateAccountResponse
                {
                    Status = StatusCode.InternalServerError
                });

            var controller = new AccountController(mockAuthenticationService.Object, mockAccountService.Object, null);

            var model = new CreateAccountViewModel();

            // act
            var result = controller.Create(model) as ViewResult;

            // assert
            Assert.IsNotNull(result);
            Assert.AreEqual(string.Empty, result.ViewName);
            Assert.IsFalse(result.ViewData.ModelState.IsValid);
            Assert.AreEqual(1, result.ViewData.ModelState.Count);
        }

        [TestMethod]
        public void Login()
        {
            // arrange
            var mockAuthenticationService = new Mock<IAuthenticationService>();
            var returnUrl = "xxxx";
            var controller = new AccountController(mockAuthenticationService.Object, null, null);

            // act
            var result = controller.LogIn(returnUrl) as ViewResult;

            // assert
            Assert.IsNotNull(result);
            Assert.AreEqual(string.Empty, result.ViewName);
            Assert.AreEqual(returnUrl, result.ViewData["returnUrl"]);
        }

        [TestMethod]
        public void Login_Post_Success()
        {
            // arrange
            var mockAuthenticationService = new Mock<IAuthenticationService>();
            mockAuthenticationService.Setup(m => m.LogIn(It.Is<LoginRequest>(x => x.UserName == "YYY" && x.Password == "ZZZ")))
                .Returns(new LoginResponse
                {
                    Status = StatusCode.OK,
                    IsAuthenticated = true
                });

            var returnUrl = "/home";
            var model = new LoginViewModel
            {
                UserName = "YYY",
                Password = "ZZZ",
            };

            var controller = new AccountController(mockAuthenticationService.Object, null, null);
            var requestContext = new RequestContext(this.mockContext.Object, new RouteData());
            controller.Url = new UrlHelper(requestContext);

            // act
            var result = controller.LogIn(model, returnUrl) as RedirectResult;

            // assert
            Assert.IsNotNull(result);
            Assert.AreEqual(returnUrl, result.Url);
        }

        [TestMethod]
        public void Login_Post_Success_InvalidReturnUrl()
        {
            // arrange
            var mockAuthenticationService = new Mock<IAuthenticationService>();
            mockAuthenticationService.Setup(m => m.LogIn(It.Is<LoginRequest>(x => x.UserName == "YYY" && x.Password == "ZZZ")))
                .Returns(new LoginResponse
                {
                    Status = StatusCode.OK,
                    IsAuthenticated = true
                });

            var returnUrl = "http://home";
            var model = new LoginViewModel
            {
                UserName = "YYY",
                Password = "ZZZ",
            };

            var controller = new AccountController(mockAuthenticationService.Object, null, null);

            // mock the UrlHelper
            var requestContext = new RequestContext(this.mockContext.Object, new RouteData());
            controller.Url = new UrlHelper(requestContext);

            // act
            var result = controller.LogIn(model, returnUrl) as RedirectToRouteResult;

            // assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.RouteValues["action"]);
            Assert.AreEqual("Home", result.RouteValues["controller"]);
        }

        [TestMethod]
        public void Login_Post_Unauthenticated()
        {
            // arrange
            var mockAuthenticationService = new Mock<IAuthenticationService>();
            mockAuthenticationService.Setup(m => m.LogIn(It.IsAny<LoginRequest>()))
                .Returns(new LoginResponse
                {
                    Status = StatusCode.Unauthorized,
                    IsAuthenticated = false
                });

            var controller = new AccountController(mockAuthenticationService.Object, null, null);

            // act
            var result = controller.LogIn(new LoginViewModel(), string.Empty) as ViewResult;

            // assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.ViewData.ModelState.IsValid);
        }

        [TestMethod]
        public void Login_Post_ServerError()
        {
            // arrange
            var mockAuthenticationService = new Mock<IAuthenticationService>();
            mockAuthenticationService.Setup(m => m.LogIn(It.IsAny<LoginRequest>()))
                .Returns(new LoginResponse
                {
                    Status = StatusCode.InternalServerError,
                    IsAuthenticated = false
                });

            var controller = new AccountController(mockAuthenticationService.Object, null, null);

            // act
            var result = controller.LogIn(new LoginViewModel(), string.Empty) as ViewResult;

            // assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.ViewData.ModelState.IsValid);
        }

        [TestMethod]
        public void LogOut()
        {
            // arrange
            var mockAuthenticationService = new Mock<IAuthenticationService>();
            var controller = new AccountController(mockAuthenticationService.Object, null, null);

            // act
            var result = controller.LogOut() as RedirectToRouteResult;

            // assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Index", result.RouteValues["action"]);
            Assert.AreEqual("Home", result.RouteValues["controller"]);
            Assert.IsNotNull(controller.TempData["SuccessMessage"]);
        }

        [TestMethod]
        public void RequestPassword()
        {
            // arrange
            var controller = new AccountController(null, null, null);

            // act
            var result = controller.RequestPassword() as ViewResult;

            // assert
            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void RequestPassword_Post()
        {
            // arrange
            var controller = new AccountController(null, null, null);
            var model = new RequestPasswordViewModel
            {

            };

            // act
            var result = controller.RequestPassword(model) as ViewResult;

            // assert
            Assert.IsNotNull(result);
        }
    }
}
