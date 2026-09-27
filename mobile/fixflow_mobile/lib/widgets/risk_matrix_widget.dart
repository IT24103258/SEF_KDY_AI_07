import 'package:flutter/material.dart';

/// Deterministic 4×4 Risk Matrix Widget for Component 2.
/// Replicates the React RiskMatrix.jsx logic exactly.
class RiskMatrixWidget extends StatelessWidget {
  final List<dynamic> assessments;
  final String? highlightImpact;
  final String? highlightLikelihood;
  final Function(String impact, String likelihood)? onCellTap;

  const RiskMatrixWidget({
    super.key,
    this.assessments = const [],
    this.highlightImpact,
    this.highlightLikelihood,
    this.onCellTap,
  });

  // Impact rows: Critical at top, Low at bottom
  static const List<String> impacts = ['Critical', 'High', 'Medium', 'Low'];
  // Likelihood columns: Low at left, Critical at right
  static const List<String> likelihoods = ['Low', 'Medium', 'High', 'Critical'];

  /// Returns the risk classification for a given impact × likelihood cell.
  /// Uses the EXACT same logic as React RiskMatrix.jsx getCellRisk.
  static String getCellRiskLevel(String impact, String likelihood) {
    final int i = impact == 'Critical' ? 4 : impact == 'High' ? 3 : impact == 'Medium' ? 2 : 1;
    final int l = likelihood == 'Critical' ? 4 : likelihood == 'High' ? 3 : likelihood == 'Medium' ? 2 : 1;
    final int score = i * l;

    if (score >= 12 || i == 4) return 'Critical';
    if (score >= 8 || i == 3) return 'High';
    if (score >= 4) return 'Medium';
    return 'Low';
  }

  static Color _riskColor(String level) {
    switch (level) {
      case 'Critical':
        return const Color(0xFFEF4444);
      case 'High':
        return const Color(0xFFF59E0B);
      case 'Medium':
        return const Color(0xFF3B82F6);
      default:
        return const Color(0xFF10B981);
    }
  }

  int _getCount(String impact, String likelihood) {
    return assessments.where((a) {
      final aImpact = (a is Map ? a['impactLevel'] : null)?.toString().toLowerCase();
      final aLikelihood = (a is Map ? a['likelihoodLevel'] : null)?.toString().toLowerCase();
      return aImpact == impact.toLowerCase() && aLikelihood == likelihood.toLowerCase();
    }).length;
  }

  @override
  Widget build(BuildContext context) {
    return Card(
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Title
            const Text(
              'Deterministic 4×4 Risk Matrix',
              style: TextStyle(fontSize: 16, fontWeight: FontWeight.w700),
            ),
            const SizedBox(height: 2),
            const Text(
              'Impact vs. Likelihood distribution',
              style: TextStyle(fontSize: 12, color: Colors.grey),
            ),
            const SizedBox(height: 12),

            // Legend Row
            Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: ['Critical', 'High', 'Medium', 'Low'].map((level) {
                final color = _riskColor(level);
                return Padding(
                  padding: const EdgeInsets.symmetric(horizontal: 6),
                  child: Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Container(
                        width: 10,
                        height: 10,
                        decoration: BoxDecoration(shape: BoxShape.circle, color: color),
                      ),
                      const SizedBox(width: 4),
                      Text(level, style: const TextStyle(fontSize: 10)),
                    ],
                  ),
                );
              }).toList(),
            ),
            const SizedBox(height: 12),

            // Axis Labels + Grid
            Row(
              crossAxisAlignment: CrossAxisAlignment.center,
              children: [
                // Y-axis label (Impact)
                const RotatedBox(
                  quarterTurns: -1,
                  child: Text('IMPACT', style: TextStyle(fontSize: 10, fontWeight: FontWeight.bold, color: Colors.grey, letterSpacing: 1.5)),
                ),
                const SizedBox(width: 4),
                // Grid
                Expanded(
                  child: Column(
                    children: [
                      // Column headers (Likelihood)
                      Row(
                        children: [
                          const SizedBox(width: 52), // row label space
                          ...likelihoods.map((l) => Expanded(
                            child: Center(
                              child: Text(l, style: const TextStyle(fontSize: 10, fontWeight: FontWeight.w600, color: Colors.grey)),
                            ),
                          )),
                        ],
                      ),
                      const SizedBox(height: 4),
                      // Matrix rows
                      ...impacts.map((imp) => Padding(
                        padding: const EdgeInsets.symmetric(vertical: 2),
                        child: Row(
                          children: [
                            SizedBox(
                              width: 52,
                              child: Text(imp, style: const TextStyle(fontSize: 11, fontWeight: FontWeight.w600, color: Colors.grey)),
                            ),
                            ...likelihoods.map((lik) {
                              final level = getCellRiskLevel(imp, lik);
                              final color = _riskColor(level);
                              final count = _getCount(imp, lik);
                              final isHighlighted = highlightImpact == imp && highlightLikelihood == lik;

                              return Expanded(
                                child: GestureDetector(
                                  onTap: onCellTap != null ? () => onCellTap!(imp, lik) : null,
                                  child: Container(
                                    margin: const EdgeInsets.all(2),
                                    padding: const EdgeInsets.symmetric(vertical: 10),
                                    decoration: BoxDecoration(
                                      color: color.withOpacity( 0.2),
                                      borderRadius: BorderRadius.circular(6),
                                      border: Border.all(
                                        color: isHighlighted ? color : color.withOpacity( 0.4),
                                        width: isHighlighted ? 2.5 : 1,
                                      ),
                                      boxShadow: isHighlighted
                                          ? [BoxShadow(color: color.withOpacity( 0.4), blurRadius: 8)]
                                          : null,
                                    ),
                                    child: Column(
                                      mainAxisSize: MainAxisSize.min,
                                      children: [
                                        Text(
                                          '$count',
                                          style: TextStyle(fontSize: 16, fontWeight: FontWeight.w800, color: color),
                                        ),
                                        Text(
                                          level.toUpperCase(),
                                          style: TextStyle(fontSize: 7, fontWeight: FontWeight.bold, color: color.withOpacity( 0.8), letterSpacing: 0.5),
                                        ),
                                      ],
                                    ),
                                  ),
                                ),
                              );
                            }),
                          ],
                        ),
                      )),
                      const SizedBox(height: 4),
                      // X-axis label
                      const Center(
                        child: Text('LIKELIHOOD', style: TextStyle(fontSize: 10, fontWeight: FontWeight.bold, color: Colors.grey, letterSpacing: 1.5)),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}
