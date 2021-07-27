using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using FluentAssertions;
using RichardSzalay.MockHttp;
using Xunit;

namespace Sanity.Client.Tests
{
    public class SanityClientTests
    {
        public class Instantiation
        {
            [Fact]
            public void ShouldThrowOnNullHttpClient()
            {
                Assert.Throws<ArgumentNullException>(() => new SanityClient(null));
            }
            
            [Fact]
            public void ShouldThrowOnNullOptions()
            {
                Assert.Throws<ArgumentNullException>(() => new SanityClient(new MockHttpMessageHandler().ToHttpClient(), null));
            }
            
            [Fact]
            public void ShouldThrowOnEmptyOptions()
            {
                Assert.Throws<ArgumentNullException>(() => new SanityClient(new MockHttpMessageHandler().ToHttpClient(), new SanityClientOptions()));
            }
            
            [Fact]
            public void ShouldThrowOnEmptyOptionsProjectId()
            {
                Assert.Throws<ArgumentNullException>(() => new SanityClient(new MockHttpMessageHandler().ToHttpClient(), new SanityClientOptions
                {
                    Dataset = "production",
                    ApiVersion = "v2021-03-25"
                }));
            }
            
            [Fact]
            public void ShouldThrowOnEmptyOptionsDataset()
            {
                Assert.Throws<ArgumentNullException>(() => new SanityClient(new MockHttpMessageHandler().ToHttpClient(), new SanityClientOptions
                {
                    ProjectId = "123456",
                    ApiVersion = "v2021-03-25"
                }));
            }
            
            [Fact]
            public void ShouldThrowOnEmptyOptionsApiVersion()
            {
                Assert.Throws<ArgumentNullException>(() => new SanityClient(new MockHttpMessageHandler().ToHttpClient(), new SanityClientOptions
                {
                    ProjectId = "123456",
                    Dataset = "production"
                }));
            }
        }
        
        public class GenericQuery
        {
            [Fact]
            public async Task GenericsQueryHappyPath()
            {
                var mockHttp = new MockHttpMessageHandler();
                mockHttp
                    .When($"https://123456.api.sanity.io/v2021-03-25/data/query/production?query={Uri.EscapeDataString("*[_type == 'article']")}")
                    .Respond("application/json", @"
                    {
                      ""query"": ""*[_type == 'article']"",
                      ""ms"": 1,
                      ""result"": [{""_id"": ""1234"", ""_type"" : ""article"", ""title"": ""title1""}]
                    }");

                var client = new SanityClient(mockHttp.ToHttpClient(), new SanityClientOptions
                {
                    ProjectId = "123456",
                    Dataset = "production",
                    ApiVersion = "v2021-03-25"
                });

                var result = await client.Query<Article[]>("*[_type == 'article']");
                result.Value.Should().HaveCount(1);
                result.Value[0].Id.Should().Be("1234");
                result.Value[0].Type.Should().Be("article");
                result.Value[0].Title.Should().Be("title1");
            }

            [Theory]
            [InlineData(HttpStatusCode.NotFound)]
            [InlineData(HttpStatusCode.InternalServerError)]
            [InlineData(HttpStatusCode.BadRequest)]
            public async Task GivenNonSuccessfulResponseHttpStatusCode_ShouldThrow(HttpStatusCode httpStatusCode)
            {
                var mockHttp = new MockHttpMessageHandler();
                mockHttp
                    .When($"https://123456.api.sanity.io/v2021-03-25/data/query/production?query={Uri.EscapeDataString("*[_type == 'article']")}")
                    .Respond(httpStatusCode);

                var client = new SanityClient(mockHttp.ToHttpClient(), new SanityClientOptions
                {
                    ProjectId = "123456",
                    Dataset = "production",
                    ApiVersion = "v2021-03-25"
                });

                await Assert.ThrowsAsync<HttpRequestException>(async () => await client.Query<Article[]>("*[_type == 'article']"));
            }

            //public async Task SimpleQuery()
            //{
            //    var client = new SanityClient.SanityClient(new System.Net.Http.HttpClient());
            //    var articles = await client.Query<Article>("*[_type == 'article']");
            //}

            //public async Task SimpleHttpResponseQuery()
            //{
            //    var client = new SanityClient.SanityClient(new System.Net.Http.HttpClient());
            //    var httpResponse = await client.Query("*[_type == 'article']");

            //    if (httpResponse.IsSuccessStatusCode())
            //    {
            //        // do something
            //    }
            //}

            //public async Task SimpleListener()
            //{
            //    var client = new SanityClient.SanityClient(new System.Net.Http.HttpClient());
            //    var listener = await client.Listen<T>("*[_type == 'article']");
            //    listener.OnEvent += HandleArticleListenerMessage;
            //    listener.OnError += HandleArticleListenerMessage;
            //    listener.Unsubscribe();
            //}

            //private Task HandleArticleListenerMessage<T>(ListenerEventArgs args)
            //{

            //}
        }
        
        public class HttpResponseQuery
        {
            [Fact]
            public async Task HappyPath()
            {
                var mockHttp = new MockHttpMessageHandler();
                mockHttp
                    .When($"https://123456.api.sanity.io/v2021-03-25/data/query/production?query={Uri.EscapeDataString("*[_type == 'article']")}")
                    .Respond("application/json", @"
                    {
                      ""query"": ""*[_type == 'article']"",
                      ""ms"": 1,
                      ""result"": [{""_id"": ""1234"", ""_type"" : ""article"", ""title"": ""title1""}]
                    }");

                var client = new SanityClient(mockHttp.ToHttpClient(), new SanityClientOptions
                {
                    ProjectId = "123456",
                    Dataset = "production",
                    ApiVersion = "v2021-03-25"
                });

                var response = await client.Query("*[_type == 'article']");
                var result = await JsonSerializer.DeserializeAsync<SanityResponse<Article[]>>(await response.Content.ReadAsStreamAsync(), new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                
                result.Value.Should().HaveCount(1);
                result.Value[0].Id.Should().Be("1234");
                result.Value[0].Type.Should().Be("article");
                result.Value[0].Title.Should().Be("title1");
            }

            [Theory]
            [InlineData(HttpStatusCode.NotFound)]
            [InlineData(HttpStatusCode.InternalServerError)]
            [InlineData(HttpStatusCode.BadRequest)]
            public async Task GivenNonSuccessfulResponseHttpStatusCode_ShouldSucceed(HttpStatusCode httpStatusCode)
            {
                var mockHttp = new MockHttpMessageHandler();
                mockHttp
                    .When($"https://123456.api.sanity.io/v2021-03-25/data/query/production?query={Uri.EscapeDataString("*[_type == 'article']")}")
                    .Respond(httpStatusCode);

                var client = new SanityClient(mockHttp.ToHttpClient(), new SanityClientOptions
                {
                    ProjectId = "123456",
                    Dataset = "production",
                    ApiVersion = "v2021-03-25"
                });

                var response = await client.Query("*[_type == 'article']");

                response.Should().NotBeNull();
            }
        }
    }

    class SanityDocument
    {
        [JsonPropertyName("_id")]
        public string Id { get; set; }

        [JsonPropertyName("_type")]
        public string Type { get; set; }
    }

    class Article : SanityDocument
    {
        public string Title { get; set; }
    }
}