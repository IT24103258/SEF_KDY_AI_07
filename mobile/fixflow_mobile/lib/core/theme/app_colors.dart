import 'package:flutter/material.dart';

class AppColors {
  // Brand Palette
  static const Color primary = Color(0xFF2457C5);
  static const Color primaryDark = Color(0xFF1D469E);
  static const Color primaryLight = Color(0xFF3B82F6);
  static const Color primaryAccent = Color(0xFFEFF6FF);

  // Status & Priority Colors
  static const Color critical = Color(0xFFDC2626);
  static const Color criticalBg = Color(0xFFFEE2E2);

  static const Color high = Color(0xFFEA580C);
  static const Color highBg = Color(0xFFFFEDD5);

  static const Color medium = Color(0xFFD97706);
  static const Color mediumBg = Color(0xFFFEF3C7);

  static const Color low = Color(0xFF16A34A);
  static const Color lowBg = Color(0xFFDCFCE7);

  static const Color inProgress = Color(0xFF2563EB);
  static const Color inProgressBg = Color(0xFFDBEAFE);

  static const Color completed = Color(0xFF15803D);
  static const Color completedBg = Color(0xFFDCFCE7);

  static const Color scheduled = Color(0xFFD97706);
  static const Color scheduledBg = Color(0xFFFEF3C7);

  static const Color paused = Color(0xFF9333EA);
  static const Color pausedBg = Color(0xFFF3E8FF);

  static const Color pending = Color(0xFF64748B);
  static const Color pendingBg = Color(0xFFF1F5F9);

  // Backgrounds & Surfaces
  static const Color lightBg = Color(0xFFF5F7FB);
  static const Color lightCard = Colors.white;
  static const Color lightBorder = Color(0xFFE2E8F0);
  static const Color lightTextPrimary = Color(0xFF0F172A);
  static const Color lightTextSecondary = Color(0xFF64748B);
  static const Color lightTextMuted = Color(0xFF94A3B8);

  static const Color darkBg = Color(0xFF172033);
  static const Color darkCard = Color(0xFF1E293B);
  static const Color darkBorder = Color(0xFF334155);
  static const Color darkTextPrimary = Color(0xFFF8FAFC);
  static const Color darkTextSecondary = Color(0xFF94A3B8);
  static const Color darkTextMuted = Color(0xFF64748B);

  // Helper methods
  static Color getPriorityColor(String priority) {
    switch (priority.toLowerCase()) {
      case 'critical':
        return critical;
      case 'high':
        return high;
      case 'medium':
        return medium;
      case 'low':
        return low;
      default:
        return medium;
    }
  }

  static Color getPriorityBgColor(String priority) {
    switch (priority.toLowerCase()) {
      case 'critical':
        return criticalBg;
      case 'high':
        return highBg;
      case 'medium':
        return mediumBg;
      case 'low':
        return lowBg;
      default:
        return mediumBg;
    }
  }

  static Color getStatusColor(String status) {
    switch (status.toLowerCase()) {
      case 'completed':
        return completed;
      case 'inprogress':
        return inProgress;
      case 'scheduled':
      case 'approved':
        return scheduled;
      case 'paused':
        return paused;
      default:
        return pending;
    }
  }

  static Color getStatusBgColor(String status) {
    switch (status.toLowerCase()) {
      case 'completed':
        return completedBg;
      case 'inprogress':
        return inProgressBg;
      case 'scheduled':
      case 'approved':
        return scheduledBg;
      case 'paused':
        return pausedBg;
      default:
        return pendingBg;
    }
  }
}
