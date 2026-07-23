using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using LoafNCatting.Application.Contracts;
using LoafNCatting.Entity.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace LoafNCatting.Tests;

[TestClass]
public sealed class MediaStorageApiTests
{
    [TestMethod]
    public async Task GetProduct_WithLegacyPicturePath_ReturnsManagedPictureKeyAndResolvedUrl()
    {
        await using var factory = new MediaStorageApiFactory();
        await factory.SeedRolesAsync();

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<LoafNcattingPrn232Context>();
            context.Categories.Add(new Category
            {
                CategoryId = 1,
                Name = "Beverages"
            });
            context.Products.Add(new Product
            {
                ProductId = 1,
                Name = "Bac Xiu",
                Description = "Legacy image path sample",
                Price = 28000m,
                UnitInStock = 12,
                Picture = "/Images/Beverages/bacxiu.jpg",
                CategoryId = 1,
                IsAvailable = true,
                CreatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        var client = factory.CreateClient();

        using var document = JsonDocument.Parse(
            await client.GetStringAsync("/api/products/1"));
        var root = document.RootElement;

        Assert.AreEqual("product/bacxiu.jpg", root.GetProperty("pictureKey").GetString());
        var picture = root.GetProperty("picture").GetString();
        Assert.IsFalse(string.IsNullOrWhiteSpace(picture));
        StringAssert.Contains(picture, "test-bucket");
        StringAssert.Contains(picture, "product/bacxiu.jpg");
    }

    [TestMethod]
    public async Task CreateProductUploadUrl_AsStaff_ReturnsPresignedUploadDescriptor()
    {
        await using var factory = new MediaStorageApiFactory();
        await factory.SeedRolesAsync();
        await factory.CreateStaffAsync();
        var client = factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest("staff@example.com", "Password1"));
        Assert.AreEqual(HttpStatusCode.OK, loginResponse.StatusCode);

        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.IsNotNull(login);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", login.AccessToken);

        var response = await client.PostAsJsonAsync("/api/uploads/product", new
        {
            fileName = "product.jpg",
            contentType = "image/jpeg",
            fileSizeBytes = 1024L
        });

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;
        var s3Key = root.GetProperty("s3Key").GetString();
        var uploadUrl = root.GetProperty("uploadUrl").GetString();
        var fileUrl = root.GetProperty("fileUrl").GetString();

        Assert.IsNotNull(s3Key);
        StringAssert.StartsWith(s3Key, "product/");
        Assert.IsFalse(string.IsNullOrWhiteSpace(uploadUrl));
        Assert.IsFalse(string.IsNullOrWhiteSpace(fileUrl));
        StringAssert.Contains(uploadUrl, "test-bucket");
        StringAssert.Contains(fileUrl, "test-bucket");
    }

    private sealed class MediaStorageApiFactory : AuthApiFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseSetting("S3:BucketName", "test-bucket");
            builder.UseSetting("S3:Region", "ap-southeast-1");
            builder.UseSetting("S3:AccessKey", "test-access-key");
            builder.UseSetting("S3:SecretKey", "test-secret-key");
            builder.UseSetting("S3:UploadUrlExpiresInMinutes", "10");
            builder.UseSetting("S3:DownloadUrlExpiresInMinutes", "60");
        }
    }
}
