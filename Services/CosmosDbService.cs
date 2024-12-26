using System.Net;
using Microsoft.Azure.Cosmos;

namespace B2S_Api.Services;

public class CosmosDbService(CosmosClient cosmosClient, string databaseId, string containerId)
{
    private readonly Container _container = cosmosClient.GetContainer(databaseId, containerId);

    public async Task AddItemAsync<T>(T item, string partitionKey)
    {
        try
        {
            await _container.CreateItemAsync(item, new PartitionKey(partitionKey));
        }
        catch (CosmosException ex)
        {
            throw new Exception("item not found: ", ex);
        }
    }

    public async Task<T?> GetItemAsync<T>(string id, string partitionKey)
    {
        try
        {
            return (await _container.ReadItemAsync<T>(id, new PartitionKey(partitionKey))).Resource;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new Exception("item not found: ", ex);
        }
    }

    public async Task<IEnumerable<T>> GetItemsAsync<T>(string query)
    {
        try
        {
            var queryIterator = _container.GetItemQueryIterator<T>(new QueryDefinition(query));
            var results = new List<T>();
            while (queryIterator.HasMoreResults)
            {
                var response = await queryIterator.ReadNextAsync();
                results.AddRange(response.Resource);
            }

            return results;
        }
        catch (Exception ex)
        {
            throw new Exception("Exception thrown while fetching cosmos items", ex);
        }
    }
    
    public async Task UpdateItemAsync<T>(string id, T updatedItem, string partitionKey)
    {
        try
        {
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(partitionKey))
            {
                throw new ArgumentException("id and partitionKey cannot be null or empty.");
            }

            // _logger.LogInformation($"Updating document with id: {id} and partitionKey: {partitionKey}");
            await _container.ReplaceItemAsync(updatedItem, id, new PartitionKey(partitionKey));
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            // _logger.LogError($"Document not found. id: {id}, partitionKey: {partitionKey}");
            throw new Exception($"Document with id '{id}' and partitionKey '{partitionKey}' was not found.", ex);
        }
        catch (Exception ex)
        {
            // _logger.LogError($"Failed to update document. id: {id}, partitionKey: {partitionKey}, Error: {ex.Message}");
            throw;
        }
    }
}