using AutoMapper;
using eCommerce.BLL.DTOs;
using eCommerce.BLL.Services;
using eCommerce.DAL.Entities;
using eCommerce.DAL.RepositoryContracts;
using FluentValidation;
using FluentValidation.Results;
using MongoDB.Driver;
using Moq;

namespace eCommerce.BLL.Tests.Services
{
    public class OrdersServiceTests
    {
        private readonly OrdersService _service;
        private readonly Mock<IOrdersRepository> _repositoryMock = new();
        private readonly Mock<IMapper> _mapperMock = new();
        private readonly Mock<IValidator<OrderAddRequest>> _addValidatorMock = new();
        private readonly Mock<IValidator<OrderUpdateRequest>> _updateValidatorMock = new();
        private readonly Mock<IValidator<OrderItemAddRequest>> _itemAddValidatorMock = new();
        private readonly Mock<IValidator<OrderItemUpdateRequest>> _itemUpdateValidatorMock = new();

        public OrdersServiceTests()
        {
            // default validators return valid
            _addValidatorMock.Setup(v => v.ValidateAsync(It.IsAny<OrderAddRequest>(), default))
                .ReturnsAsync(new ValidationResult());
            _updateValidatorMock.Setup(v => v.ValidateAsync(It.IsAny<OrderUpdateRequest>(), default))
                .ReturnsAsync(new ValidationResult());
            _itemAddValidatorMock.Setup(v => v.ValidateAsync(It.IsAny<OrderItemAddRequest>(), default))
                .ReturnsAsync(new ValidationResult());
            _itemUpdateValidatorMock.Setup(v => v.ValidateAsync(It.IsAny<OrderItemUpdateRequest>(), default))
                .ReturnsAsync(new ValidationResult());

            _service = new OrdersService(
                _repositoryMock.Object,
                _mapperMock.Object,
                _addValidatorMock.Object,
                _updateValidatorMock.Object,
                _itemAddValidatorMock.Object,
                _itemUpdateValidatorMock.Object
            );
        }

        [Fact]
        public async Task AddOrderAsync_ShouldReturnMappedOrderResponse_WhenValidRequest()
        {
            // Arrange
            var request = new OrderAddRequest
            {
                UserId = Guid.NewGuid(),
                Items = new List<OrderItemAddRequest>
                {
                    new() { ProductId = Guid.NewGuid(), Quantity = 1, UnitPrice= 100 }
                }
            };

            var orderEntity = new Order();
            var orderResponse = new OrderResponse();

            _mapperMock.Setup(m => m.Map<Order>(request)).Returns(orderEntity);
            _mapperMock.Setup(m => m.Map<List<OrderItem>>(request.Items)).Returns(new List<OrderItem>());
            _repositoryMock.Setup(r => r.AddOrderAsync(orderEntity)).ReturnsAsync(orderEntity);
            _mapperMock.Setup(m => m.Map<OrderResponse>(orderEntity)).Returns(orderResponse);

            // Act
            var result = await _service.AddOrderAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(orderResponse, result);
        }

        [Fact]
        public async Task AddOrderAsync_ShouldThrowArgumentException_WhenOrderInvalid()
        {
            var request = new OrderAddRequest { Items = new List<OrderItemAddRequest>() };
            _addValidatorMock.Setup(v => v.ValidateAsync(request, default))
                .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("UserId", "Required") }));

            await Assert.ThrowsAsync<ArgumentException>(() => _service.AddOrderAsync(request));
        }

        [Fact]
        public async Task UpdateOrderAsync_ShouldReturnMappedOrderResponse_WhenOrderExists()
        {
            var request = new OrderUpdateRequest
            {
                OrderId = Guid.NewGuid(),
                Items = new List<OrderItemUpdateRequest> { new() { ProductId = Guid.NewGuid(), Quantity = 2, UnitPrice= 200 } }
            };

            var orderEntity = new Order();
            var orderResponse = new OrderResponse();

            _repositoryMock.Setup(r => r.GetSingleOrderByConditionAsync(It.IsAny<FilterDefinition<Order>>()))
                .ReturnsAsync(orderEntity);
            _mapperMock.Setup(m => m.Map<Order>(request)).Returns(orderEntity);
            _mapperMock.Setup(m => m.Map<List<OrderItem>>(request.Items)).Returns(new List<OrderItem>());
            _repositoryMock.Setup(r => r.UpdateOrderAsync(orderEntity)).ReturnsAsync(orderEntity);
            _mapperMock.Setup(m => m.Map<OrderResponse>(orderEntity)).Returns(orderResponse);

            var result = await _service.UpdateOrderAsync(request);

            Assert.NotNull(result);
            Assert.Equal(orderResponse, result);
        }

        [Fact]
        public async Task UpdateOrderAsync_ShouldReturnNull_WhenOrderDoesNotExist()
        {
            var request = new OrderUpdateRequest { OrderId = Guid.NewGuid(), Items = new List<OrderItemUpdateRequest>() };
            _repositoryMock.Setup(r => r.GetSingleOrderByConditionAsync(It.IsAny<FilterDefinition<Order>>()))
                .ReturnsAsync((Order)null);

            var result = await _service.UpdateOrderAsync(request);

            Assert.Null(result);
        }

        [Fact]
        public async Task DeleteOrderAsync_ShouldReturnTrue_WhenOrderExists()
        {
            var orderId = Guid.NewGuid();
            _repositoryMock.Setup(r => r.GetSingleOrderByConditionAsync(It.IsAny<FilterDefinition<Order>>()))
                .ReturnsAsync(new Order());
            _repositoryMock.Setup(r => r.DeleteOrderAsync(orderId)).ReturnsAsync(true);

            var result = await _service.DeleteOrderAsync(orderId);

            Assert.True(result);
        }

        [Fact]
        public async Task DeleteOrderAsync_ShouldReturnFalse_WhenOrderDoesNotExist()
        {
            var orderId = Guid.NewGuid();
            _repositoryMock.Setup(r => r.GetSingleOrderByConditionAsync(It.IsAny<FilterDefinition<Order>>()))
                .ReturnsAsync((Order)null);

            var result = await _service.DeleteOrderAsync(orderId);

            Assert.False(result);
        }

        [Fact]
        public async Task GetSingleOrderByConditionAsync_ShouldThrowKeyNotFoundException_WhenOrderNotFound()
        {
            _repositoryMock.Setup(r => r.GetSingleOrderByConditionAsync(It.IsAny<FilterDefinition<Order>>()))
                .ReturnsAsync((Order)null);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.GetSingleOrderByConditionAsync(Builders<Order>.Filter.Empty));
        }

        [Fact]
        public async Task GetMultipleOrdersByConditionAsync_ShouldReturnMappedOrders()
        {
            var orders = new List<Order> { new() };
            var orderResponses = new List<OrderResponse> { new() };

            _repositoryMock.Setup(r => r.GetMultipleOrdersByConditionAsync(It.IsAny<FilterDefinition<Order>>()))
                .ReturnsAsync(orders);
            _mapperMock.Setup(m => m.Map<List<OrderResponse>>(orders)).Returns(orderResponses);

            var result = await _service.GetMultipleOrdersByConditionAsync(Builders<Order>.Filter.Empty);

            Assert.Equal(orderResponses, result);
        }

        [Fact]
        public async Task GetOrdersAsync_ShouldReturnMappedOrders()
        {
            var orders = new List<Order> { new() };
            var orderResponses = new List<OrderResponse> { new() };

            _repositoryMock.Setup(r => r.GetOrdersAsync()).ReturnsAsync(orders);
            _mapperMock.Setup(m => m.Map<List<OrderResponse>>(orders)).Returns(orderResponses);

            var result = await _service.GetOrdersAsync();

            Assert.Equal(orderResponses, result);
        }
    }
}
