// using AutoMapper;
// using webapi_blazor.models.EbayDB;
// using System.Text.Json;

// public class MappingProfile : Profile
// {
//     public MappingProfile()
//     {
//         // Ánh xạ từ User sang UserDTO
//         // CreateMap<GetListOrderDetailByOrderId, OrderItemVM>()
//         //         .ForMember(
//         //             dest => dest.lstOrderDetail,
//         //             opt => opt.MapFrom(src => JsonSerializer.Deserialize<List<ItemOrderVM>>(src.OrderDetail, new JsonSerializerOptions()))
//         //         ).ForMember(dest => dest.Id, opt => opt.Ignore());

//         CreateMap<GetListOrderDetailByOrderId, OrderItemVM>()
//         .ConvertUsing(src => new OrderItemVM
//         {
//             lstOrderDetail = JsonSerializer.Deserialize<List<ItemOderVM>>(src.OrderDetail, new JsonSerializerOptions()),
//             Id = src.Id,
//             CreatedAt = src.CreatedAt,
//             // OrderDetail sẽ tự động không được ánh xạ vì không gán
//         });

//         // .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.FullName));
//         // Ánh xạ ngược lại từ UserDTO về User
//         //   CreateMap<UserDTO, UserEntity>()
//         //     .ForMember(dest => dest.CreatedAt, opt => opt.Ignore()); // Không ánh xạ CreatedAt
//     }
// }
