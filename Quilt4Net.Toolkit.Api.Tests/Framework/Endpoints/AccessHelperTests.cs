//using Xunit;

//namespace Quilt4Net.Toolkit.Api.Tests.Framework.Endpoints;

//public class AccessHelperTests
//{
//    [Fact]
//    public void Decode()
//    {
//        //Arrange
//        var endpoints = "6666644";

//        //Act
//        var result = AccessHelper.Decode(endpoints);

//        //Assert
//        result.First().Value.Get.Should().BeTrue();
//        result.First().Value.Head.Should().BeTrue();
//        result.First().Value.Visible.Should().BeTrue();
//    }

//    [Theory]
//    [InlineData("")]
//    [InlineData(null)]
//    public void Empty(string endpoints)
//    {
//        //Arrange

//        //Act
//        var result = AccessHelper.Decode(endpoints);

//        //Assert
//        result.First().Value.Get.Should().BeFalse();
//        result.First().Value.Head.Should().BeFalse();
//        result.First().Value.Visible.Should().BeFalse();
//        result.Encode().Should().Be("0000000");
//    }

//    [Theory]
//    [InlineData("1")]
//    public void Short(string endpoints)
//    {
//        //Arrange

//        //Act
//        var result = AccessHelper.Decode(endpoints);

//        //Assert
//        result.First().Value.Get.Should().BeTrue();
//        result.First().Value.Head.Should().BeFalse();
//        result.First().Value.Visible.Should().BeFalse();
//        result.Encode().Should().Be("1000000");
//    }

//    [Theory]
//    [InlineData("111111111111111111")]
//    public void Long(string endpoints)
//    {
//        //Arrange

//        //Act
//        var result = AccessHelper.Decode(endpoints);

//        //Assert
//        result.First().Value.Get.Should().BeTrue();
//        result.First().Value.Head.Should().BeFalse();
//        result.First().Value.Visible.Should().BeFalse();
//        result.Encode().Should().Be("1111111");
//    }

//    [Theory]
//    [InlineData("7")]
//    [InlineData("x")]
//    public void Invalid(string endpoints)
//    {
//        //Arrange

//        //Act
//        Assert.Throws<ArgumentException>(() => AccessHelper.Decode(endpoints));

//        //Assert
//    }
//}