﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AdidasStoreMVC.Models
{
    public class Order
    {
        public int Id { get; set; }

        [Required]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Address { get; set; } = string.Empty;

        [Required]
        public string Phone { get; set; } = string.Empty;

        public DateTime OrderDate { get; set; } = DateTime.Now;

        public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        public decimal TotalAmount { get; set; }
    }
    public enum OrderStatus
    {
        Pending,    // Đơn mới, chưa duyệt
        Approved,   // Đã duyệt
        Rejected    // Đã từ chối
    }
}