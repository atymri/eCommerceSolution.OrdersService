using AutoMapper;
using eCommerce.BLL.DTOs;
using eCommerce.BusinessLogicLayer.DTOs;
using eCommerce.DAL.Entities;

namespace eCommerce.BLL.Profiles;

internal class OrderItemProfile : Profile
{
    public OrderItemProfile()
    {
        CreateMap<OrderItemAddRequest, OrderItem>()
            .ForMember(dst => dst.ProductID, opt => opt.MapFrom(src => src.ProductId))
            .ForMember(dst => dst.Quantity, opt => opt.MapFrom(src => src.Quantity))
            .ForMember(dst => dst.TotalPrice, opt => opt.Ignore())
            .ForMember(dst => dst._id, opt => opt.Ignore());

        CreateMap<OrderItemUpdateRequest, OrderItem>()
            .ForMember(dst => dst.ProductID, opt => opt.MapFrom(src => src.ProductId))
            .ForMember(dst => dst.Quantity, opt => opt.MapFrom(src => src.Quantity))
            .ForMember(dst => dst.TotalPrice, opt => opt.Ignore());

        CreateMap<OrderItem, OrderItemResponse>()
            .ForMember(dst => dst.ProductId, opt => opt.MapFrom(src => src.ProductID))
            .ForMember(dst => dst.Quantity, opt => opt.MapFrom(src => src.Quantity))
            .ForMember(dst => dst.TotalPrice, opt => opt.MapFrom(src => src.TotalPrice));

        CreateMap<ProductSummery, OrderItemResponse>()
            .ForMember(dst => dst.ProductId, opt => opt.MapFrom(src => src.ProductID))
            .ForMember(dst => dst.ProductName, opt => opt.MapFrom(src => src.ProductName))
            .ForMember(dst => dst.Category, opt => opt.MapFrom(src => src.Category));
        
    }
}
