using AutoMapper;
using eCommerce.BLL.DTOs;
using eCommerce.DAL.Entities;

namespace eCommerce.BLL.Profiles;

internal class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<OrderAddRequest, Order>()
            .ForMember(dst => dst.OrderItems, opt => opt.MapFrom(src => src.Items))
            .ForMember(dst => dst.UserID, opt => opt.MapFrom(src => src.UserId))
            .ForMember(dst => dst.OrderDate, opt => opt.MapFrom(src => src.OrderDate))
            .ForMember(dst => dst.TotalBill, opt => opt.Ignore())
            .ForMember(dst => dst.OrderID, opt => opt.Ignore());

        CreateMap<OrderUpdateRequest, Order>()
            .ForMember(dst => dst.OrderItems, opt => opt.MapFrom(src => src.Items))
            .ForMember(dst => dst.UserID, opt => opt.MapFrom(src => src.UserId))
            .ForMember(dst => dst.OrderDate, opt => opt.MapFrom(src => src.OrderDate))
            .ForMember(dst => dst.TotalBill, opt => opt.Ignore())
            .ForMember(dst => dst.OrderID, opt => opt.MapFrom(src => src.OrderId));

        CreateMap<Order, OrderResponse>()
            .ForMember(dst => dst.Items, opt => opt.MapFrom(src => src.OrderItems))
            .ForMember(dst => dst.UserId, opt => opt.MapFrom(src => src.UserID))
            .ForMember(dst => dst.OrderDate, opt => opt.MapFrom(src => src.OrderDate))
            .ForMember(dst => dst.TotalBill, opt => opt.MapFrom(src => src.TotalBill))
            .ForMember(dst => dst.OrderId, opt => opt.MapFrom(src => src.OrderID))
            .ForMember(dst => dst.PersonName, opt => opt.Ignore())
            .ForMember(dst => dst.Email, opt => opt.Ignore());

        CreateMap<UserRersponse, OrderResponse>()
            .ForMember(dst => dst.PersonName, opt => opt.MapFrom(src => src.PersonName))
            .ForMember(dst => dst.Email, opt => opt.MapFrom(src => src.Email))
            .ForMember(dst => dst.Items, opt => opt.Ignore())
            .ForMember(dst => dst.UserId, opt => opt.Ignore())
            .ForMember(dst => dst.OrderDate, opt => opt.Ignore())
            .ForMember(dst => dst.TotalBill, opt => opt.Ignore())
            .ForMember(dst => dst.OrderId, opt => opt.Ignore());



    }
}
