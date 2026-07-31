// ─────────────────────────────────────────────────────────────────────────────
//  Global usings — dùng chung cho TOÀN BỘ project test.
//  Gom các namespace lặp lại vào đây để mỗi file test gọn gàng, dễ đọc.
//  (Bật <ImplicitUsings> nên các using hệ thống như System.* đã có sẵn.)
// ─────────────────────────────────────────────────────────────────────────────

// Test frameworks
global using Xunit;
global using Moq;
global using FluentAssertions;

// Hạ tầng test dùng chung (TestKit)
global using LuminaTutors.UnitTests.TestKit;

// Các kiểu nền tảng của Domain hay dùng trong test
global using LuminaTutors.Domain.Common;
global using LuminaTutors.Domain.Enums;
global using LuminaTutors.Domain.Interfaces.Repositories;
