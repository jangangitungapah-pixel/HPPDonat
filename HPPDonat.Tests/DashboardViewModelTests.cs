using Xunit;
using Moq;
using HPPDonat.Services;
using HPPDonat.ViewModels;
using System;

namespace HPPDonat.Tests
{
    public class DashboardViewModelTests
    {
        [Fact]
        public void Greeting_ShouldReturnCorrectGreetingBasedOnTime()
        {
            // Arrange
            var mockStateService = new Mock<IAppStateService>();
            var viewModel = new DashboardViewModel(mockStateService.Object);

            // Act
            var greeting = viewModel.Greeting;

            // Assert
            var hour = DateTime.Now.Hour;
            string expected;
            if (hour < 12) expected = "Selamat Pagi";
            else if (hour < 18) expected = "Selamat Siang";
            else expected = "Selamat Malam";

            Assert.Equal(expected, greeting);
        }

        [Fact]
        public void CurrentDate_ShouldReturnFormattedDate()
        {
            // Arrange
            var mockStateService = new Mock<IAppStateService>();
            var viewModel = new DashboardViewModel(mockStateService.Object);

            // Act
            var date = viewModel.CurrentDate;

            // Assert
            Assert.Equal(DateTime.Now.ToString("dddd, dd MMMM yyyy"), date);
        }
    }
}
