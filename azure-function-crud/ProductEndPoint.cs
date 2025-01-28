using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Serialization.HybridRow.Schemas;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace azure_function_crud
{
    public class ProductEndPoint
    {
        private readonly ILogger<ProductEndPoint> _logger;
        private readonly CosmosClient _cosmosClient;
        private Container documentContainer;
        public ProductEndPoint(ILogger<ProductEndPoint> logger, CosmosClient cosmosClient)
        {
            _logger = logger;
            _cosmosClient = cosmosClient;
            documentContainer = _cosmosClient.GetContainer("ProductDB", "Items");
        }

        [Function("CreateProduct")]
        public async Task<IActionResult> CreateProductItem(
           [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "product")] HttpRequestData req)
        {
            if (req == null)
            {
                _logger.LogError("HttpRequest is null");
                return new BadRequestResult();
            }
            _logger.LogInformation("Creating Product Item");
            string requestData = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<Product>(requestData);

            var item = new Product
            {
                Name = data.Name,
                Category = data.Category,
                Quantity = data.Quantity,
                Price = data.Price,
                Description = data.Description
            };

            await documentContainer.CreateItemAsync(item, new Microsoft.Azure.Cosmos.PartitionKey(item.Category));

            return new OkObjectResult(item);
        }

        [Function("GetAllProducts")]
        public async Task<IActionResult> GetShoppingCartItems(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "product")] HttpRequestData req)
        {
            _logger.LogInformation("Getting All Product Items");

            var items = documentContainer.GetItemQueryIterator<Product>();
            return new OkObjectResult((await items.ReadNextAsync()).ToList());
        }

        [Function("GetProductItemById")]
        public async Task<IActionResult> GetShoppingCartItemById(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "product/{id}/{category}")]

            HttpRequest req,string id, string category)
        {
            _logger.LogInformation($"Getting Product Item with ID: {id}");

            try
            {
                var item = await documentContainer.ReadItemAsync<Product>(id, new Microsoft.Azure.Cosmos.PartitionKey(category));
                return new OkObjectResult(item.Resource);
            }
            catch (CosmosException e) when (e.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return new NotFoundResult();
            }
        }

        [Function("UpdateProduct")]
        public async Task<IActionResult> PutProductItem(
            [HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "product/{id}/{category}")] HttpRequestData req,
            string id, string category)
        {
            _logger.LogInformation($"Updating Product Item with ID: {id}");

            try
            {
                // Read request body
                string requestData = await new StreamReader(req.Body).ReadToEndAsync();
                var data = JsonConvert.DeserializeObject<Product>(requestData);
                var partitionKey = new Microsoft.Azure.Cosmos.PartitionKey(category);

                // Fetch the existing item from Cosmos DB
                var itemResponse = await documentContainer.ReadItemAsync<Product>(id, partitionKey);

                if (itemResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return new NotFoundResult();
                }

                // Update fields in the existing item
                var existingItem = itemResponse.Resource;
                existingItem.Updated = data.Updated;

                existingItem.Name = data.Name ?? existingItem.Name;
                existingItem.Price = data.Price ?? existingItem.Price;
                existingItem.Description = data.Description ?? existingItem.Description;

                // Upsert the updated product
                var upsertResponse = await documentContainer.UpsertItemAsync(existingItem, partitionKey);


                // Explicitly fetch the updated item to ensure latest data
                var updatedItemResponse = await documentContainer.ReadItemAsync<Product>(id, new Microsoft.Azure.Cosmos.PartitionKey(category));

                // Return the updated item
                return new OkObjectResult(updatedItemResponse.Resource);
            }
            catch (Microsoft.Azure.Cosmos.CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning($"Product with ID: {id} not found.");
                return new NotFoundResult();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating product item: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        [Function("DeleteProduct")]
        public async Task<IActionResult> DeleteProductItem(
            [HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "product/{id}/{category}")] HttpRequestData req, 
            string id, string category)
        {
            _logger.LogInformation($"Deleting Product Item with ID: {id}");

            await documentContainer.DeleteItemAsync<Product>(id, new Microsoft.Azure.Cosmos.PartitionKey(category));
            return new OkResult();
        }
    }
}
