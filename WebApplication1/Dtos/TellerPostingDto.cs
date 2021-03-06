﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace WebApplication1.Dtos
{
    public class TellerPostingDto
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [Display(Name = "Posting Type")]
        public string PostingType { get; set; }

        [Required]
        [Display(Name = "Customer Account")]
        public int CustomerAccountId { get; set; }

        [Required]
        public long Amount { get; set; }

        [Required]
        public string Narration { get; set; }

        public int TellerId { get; set; }
        [Required]
        public DateTime TransactionDate { get; set; }
    }
}