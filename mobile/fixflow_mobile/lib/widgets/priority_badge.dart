import 'package:flutter/material.dart';

class PriorityBadgeWidget extends StatelessWidget {
  final String priority;

  const PriorityBadgeWidget({super.key, required this.priority});

  Color _getColor() {
    switch (priority.toLowerCase()) {
      case 'critical':
        return const Color(0xFFDC2626);
      case 'high':
        return const Color(0xFFF59E0B);
      case 'medium':
        return const Color(0xFF2563EB);
      case 'low':
      default:
        return const Color(0xFF10B981);
    }
  }

  @override
  Widget build(BuildContext context) {
    final color = _getColor();
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.15),
        border: Border.all(color: color.withValues(alpha: 0.4)),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Text(
        priority.toUpperCase(),
        style: TextStyle(
          color: color,
          fontSize: 11,
          fontWeight: FontWeight.bold,
          letterSpacing: 0.5,
        ),
      ),
    );
  }
}

class RiskLevelBadgeWidget extends StatelessWidget {
  final String riskLevel;

  const RiskLevelBadgeWidget({super.key, required this.riskLevel});

  Color _getColor() {
    switch (riskLevel.toLowerCase()) {
      case 'critical':
        return const Color(0xFFDC2626);
      case 'high':
        return const Color(0xFFF59E0B);
      case 'medium':
        return const Color(0xFF2563EB);
      case 'low':
      default:
        return const Color(0xFF10B981);
    }
  }

  @override
  Widget build(BuildContext context) {
    final color = _getColor();
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(
        color: color.withValues(alpha: 0.12),
        border: Border.all(color: color.withValues(alpha: 0.3)),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Text(
        'RISK: ${riskLevel.toUpperCase()}',
        style: TextStyle(
          color: color,
          fontSize: 11,
          fontWeight: FontWeight.bold,
        ),
      ),
    );
  }
}

class SLAResponseCardWidget extends StatelessWidget {
  final String window;
  final int responseHours;
  final int resolutionHours;

  const SLAResponseCardWidget({
    super.key,
    required this.window,
    required this.responseHours,
    required this.resolutionHours,
  });

  @override
  Widget build(BuildContext context) {
    return Card(
      elevation: 0,
      color: Colors.blue.withValues(alpha: 0.06),
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(8),
        side: BorderSide(color: Colors.blue.withValues(alpha: 0.2)),
      ),
      child: Padding(
        padding: const EdgeInsets.all(12.0),
        child: Row(
          children: [
            const Icon(Icons.timer_outlined, color: Color(0xFF2563EB), size: 28),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    'SLA Response: $window',
                    style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 13),
                  ),
                  const SizedBox(height: 2),
                  Text(
                    'Target: ${responseHours}h dispatch • ${resolutionHours}h resolution',
                    style: const TextStyle(fontSize: 11, color: Colors.grey),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}
