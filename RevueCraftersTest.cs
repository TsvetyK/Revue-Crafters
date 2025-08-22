using System;
using System.Net;
using System.Text.Json;
using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using RevueCrafters.Models;






namespace RevueCrafters
{
    [TestFixture]
    public class RevueCraftersTests
    {
        private RestClient client;
        private static string lastCreatedRevueId;

        private const string BaseUrl = "https://d2925tksfvgq8c.cloudfront.net";

        private const string StaticToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJKd3RTZXJ2aWNlQWNjZXNzVG9rZW4iLCJqdGkiOiJmMWNhM2FhMi1jZWFkLTQ4ZjUtYWUwZS1mMDgxMTljOWU3MTQiLCJpYXQiOiIwOC8yMi8yMDI1IDA2OjMzOjQ3IiwiVXNlcklkIjoiNWY4NzJlOGEtZGZiNy00NzBjLTEzMzItMDhkZGRlMWQ4YTY0IiwiRW1haWwiOiJ0c3ZldHlAZXhhbXBsZS5jb20iLCJVc2VyTmFtZSI6InRzdmV0eSIsImV4cCI6MTc1NTg2NjAyNywiaXNzIjoiUmV2dWVNYWtlcl9BcHBfU29mdFVuaSIsImF1ZCI6IlJldnVlTWFrZXJfV2ViQVBJX1NvZnRVbmkifQ.gXw0rzl11mxkGrfVv2LeABXp2Ksgm4UBCl_OTD-kN-s";

        private const string LoginEmail = "tsvety@example.com";
        private const string LoginPassword = "123123tsvety";

        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken;

            if (!string.IsNullOrWhiteSpace(StaticToken))
            {
                jwtToken = StaticToken;
            }
            else
            {
                jwtToken = GetJwtToken(LoginEmail, LoginPassword);
            }

            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(jwtToken),
            };

            this.client = new RestClient(options);
        }

        private string GetJwtToken(string email, string password)
        {
            var tempCLient = new RestClient(BaseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { email, password });

            var response = tempCLient.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = content.GetProperty("accessToken").GetString();

                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Failed to retrieve JWT token from the response.");
                }
                return token;
            }
            else
            {
                throw new InvalidOperationException($"Failed to authenticate. Status code: {response.StatusCode}, Content: {response.Content}");
            }
        }

            [Order(1)]
            [Test]
            public void CreateNewRevue_WithRequiredFields_ShouldReturnSuccessfullyCreated()
            {
                var revueRequest = new RevueDTO
                {
                    Title = "New Revue",
                    Url = "",
                    Description = "Revue Description."
                };

                var request = new RestRequest("/api/Revue/Create", Method.Post);
                request.AddJsonBody(revueRequest);
                var response = this.client.Execute(request);
                var createResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(createResponse.Msg, Is.EqualTo("Successfully created!"));
            }

        [Order(2)]
        [Test]
        public void GetAllRevues_ShouldReturnListOfRevues()
        {
            var request = new RestRequest("/api/Revue/All", Method.Get);
            var response = client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), response.Content);

            var revues = JsonSerializer.Deserialize<JsonElement[]>(response.Content);
            Assert.That(revues, Is.Not.Null.And.Not.Empty);


            var item = revues[0];


            lastCreatedRevueId = GetStringPropCI(item, "revueId", "id");
            Assert.That(lastCreatedRevueId, Is.Not.Null.And.Not.Empty);
        }

        private static string? GetStringPropCI(JsonElement obj, params string[] names)
        {
            foreach (var a in obj.EnumerateObject())
            {
                foreach (var n in names)
                {
                    if (string.Equals(a.Name, n, StringComparison.OrdinalIgnoreCase) &&
                        a.Value.ValueKind == JsonValueKind.String)
                    {
                        return a.Value.GetString();
                    }
                }
            }
            return null;
        }


        [Order(3)]
        [Test]

        public void EditExistingRevue_ShouldReturnSuccess()
        {
            Assume.That(!string.IsNullOrEmpty(lastCreatedRevueId));

            var editRequest = new 
            { 
                Title = "Edited", 
                Url = "", 
                Description = "Edit revue description." 
            };

            var request = new RestRequest("/api/Revue/Edit", Method.Put);
            request.AddQueryParameter("revueId", lastCreatedRevueId);
            request.AddJsonBody(editRequest);

            var response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), response.Content);

            var editResponse = JsonSerializer.Deserialize<JsonElement>(response.Content);
            var msg = editResponse.GetProperty("msg").GetString();
            Assert.That(msg, Is.EqualTo("Edited successfully"));
        }


        [Order(4)]
        [Test]
        public void DeleteRevue_ShouldReturnSuccess()
        {
            var request = new RestRequest($"/api/Revue/Delete", Method.Delete);
            request.AddQueryParameter("revueId", lastCreatedRevueId);
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Does.Contain("The revue is deleted!"));
        }

        [Order(5)]
        [Test]

        public void CreateRevue_WithoutRequiredFields_ShouldReturnBadRequest()
        {
            var revueRequest = new RevueDTO
            {
                Title = "",
                Description = ""
            };

            var request = new RestRequest("/api/Revue/Create", Method.Post);
            request.AddJsonBody(revueRequest);
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

        }

        [Order(6)]
        [Test]

        public void EditNonExistingRevue_ShouldReturnNotFound()
        {
            string nonExistingRevueId = "tsve";
            var editRequest = new RevueDTO
            {
                Title = "Edited fake revue",
                Description = "Description for a edited fake revue.",
                Url = ""
            };
            var request = new RestRequest($"/api/Revue/Edit", Method.Put);
            request.AddQueryParameter("revueId", nonExistingRevueId);
            request.AddJsonBody(editRequest);
            var response = this.client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Content, Does.Contain("There is no such revue!"));
        }

        [Order(7)]
        [Test]

        public void DeleteNonExistingRevue_ShouldReturnNotFound()
        {
            string nonExistingRevueId = "tsv";
            var request = new RestRequest($"/api/Revue/Delete", Method.Delete);
            request.AddQueryParameter("revueId", nonExistingRevueId);
            var response = this.client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Content, Does.Contain("There is no such revue!"));
        }

        [OneTimeTearDown]
        public void Teardown()
        {
            this.client?.Dispose();
        }
    }
}