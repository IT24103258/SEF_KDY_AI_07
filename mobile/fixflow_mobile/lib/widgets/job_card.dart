import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import '../core/theme/app_colors.dart';
import '../models/work_order_model.dart';
import 'priority_badge.dart';
import 'status_badge.dart';

class JobCard extends StatelessWidget {
  final WorkOrderModel job;
  final VoidCallback onDetails;
  final VoidCallback? onStart;
  final bool showActions;

  const JobCard({
    super.key,
    required this.job,
    required this.onDetails,
    this.onStart,
    this.showActions = true,
  });

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final priorityColor = AppColors.getPriorityColor(job.priority);
    final scheduledWindow = job.scheduledStartTime == null
        ? 'Not scheduled'
        : job.scheduledEndTime == null
            ? 'Starts ${DateFormat('hh:mm a').format(job.scheduledStartTime!)}'
            : '${DateFormat('hh:mm a').format(job.scheduledStartTime!)} - ${DateFormat('hh:mm a').format(job.scheduledEndTime!)}';

    final isCompleted = job.status.toLowerCase() == 'completed';
    final isInProgress = job.status.toLowerCase() == 'inprogress';

    return Card(
      margin: const EdgeInsets.only(bottom: 14),
      clipBehavior: Clip.antiAlias,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
        side: BorderSide(
          color: job.conflictDetected
              ? AppColors.critical
              : isDark
                  ? AppColors.darkBorder
                  : AppColors.lightBorder,
          width: job.conflictDetected ? 1.5 : 1.0,
        ),
      ),
      child: InkWell(
        onTap: onDetails,
        child: IntrinsicHeight(
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              // Distinct colored leading indicator bar
              Container(
                width: 6,
                color:
                    job.conflictDetected ? AppColors.critical : priorityColor,
              ),

              // Card Content
              Expanded(
                child: Padding(
                  padding: const EdgeInsets.all(16.0),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      // Top Row: Time & Badges
                      Row(
                        crossAxisAlignment: CrossAxisAlignment.center,
                        children: [
                          Icon(
                            Icons.access_time_filled,
                            size: 14,
                            color: isDark
                                ? AppColors.darkTextMuted
                                : AppColors.lightTextMuted,
                          ),
                          const SizedBox(width: 4),
                          Text(
                            scheduledWindow,
                            style: TextStyle(
                              fontSize: 12,
                              fontWeight: FontWeight.w600,
                              color: isDark
                                  ? AppColors.darkTextSecondary
                                  : AppColors.lightTextSecondary,
                            ),
                          ),
                          const Spacer(),
                          PriorityBadge(priority: job.priority),
                          const SizedBox(width: 6),
                          StatusBadge(status: job.status),
                        ],
                      ),
                      const SizedBox(height: 10),

                      // Work Order Number & Conflict Warning
                      Row(
                        children: [
                          Text(
                            job.workOrderNumber,
                            style: TextStyle(
                              fontSize: 12,
                              fontWeight: FontWeight.w700,
                              color: isDark
                                  ? AppColors.primaryLight
                                  : AppColors.primary,
                            ),
                          ),
                          if (job.conflictDetected) ...[
                            const SizedBox(width: 6),
                            Container(
                              padding: const EdgeInsets.symmetric(
                                  horizontal: 6, vertical: 2),
                              decoration: BoxDecoration(
                                color: AppColors.criticalBg,
                                borderRadius: BorderRadius.circular(4),
                              ),
                              child: Row(
                                mainAxisSize: MainAxisSize.min,
                                children: const [
                                  Icon(Icons.warning_amber_rounded,
                                      color: AppColors.critical, size: 12),
                                  SizedBox(width: 2),
                                  Text(
                                    'CONFLICT',
                                    style: TextStyle(
                                      fontSize: 9,
                                      fontWeight: FontWeight.bold,
                                      color: AppColors.critical,
                                    ),
                                  ),
                                ],
                              ),
                            ),
                          ],
                        ],
                      ),
                      const SizedBox(height: 4),

                      // Maintenance Issue Title
                      Text(
                        job.title,
                        style: TextStyle(
                          fontSize: 15,
                          fontWeight: FontWeight.bold,
                          color: isDark
                              ? AppColors.darkTextPrimary
                              : AppColors.lightTextPrimary,
                        ),
                        maxLines: 2,
                        overflow: TextOverflow.ellipsis,
                      ),
                      const SizedBox(height: 8),

                      // Location
                      Row(
                        children: [
                          Icon(
                            Icons.location_on_outlined,
                            size: 15,
                            color: isDark
                                ? AppColors.darkTextMuted
                                : AppColors.lightTextMuted,
                          ),
                          const SizedBox(width: 4),
                          Expanded(
                            child: Text(
                              job.formattedLocation,
                              style: TextStyle(
                                fontSize: 13,
                                color: isDark
                                    ? AppColors.darkTextSecondary
                                    : AppColors.lightTextSecondary,
                              ),
                              overflow: TextOverflow.ellipsis,
                            ),
                          ),
                        ],
                      ),
                      if (showActions) ...[
                        const SizedBox(height: 14),
                        Row(
                          children: [
                            Expanded(
                              child: OutlinedButton(
                                onPressed: onDetails,
                                style: OutlinedButton.styleFrom(
                                  padding:
                                      const EdgeInsets.symmetric(vertical: 10),
                                  side: BorderSide(
                                    color: isDark
                                        ? AppColors.darkBorder
                                        : AppColors.lightBorder,
                                  ),
                                  foregroundColor: isDark
                                      ? AppColors.darkTextPrimary
                                      : AppColors.lightTextPrimary,
                                ),
                                child: const Text('Details',
                                    style: TextStyle(fontSize: 13)),
                              ),
                            ),
                            const SizedBox(width: 10),
                            Expanded(
                              child: ElevatedButton(
                                onPressed: isCompleted ? onDetails : onStart,
                                style: ElevatedButton.styleFrom(
                                  padding:
                                      const EdgeInsets.symmetric(vertical: 10),
                                  backgroundColor: isCompleted
                                      ? AppColors.completed
                                      : isInProgress
                                          ? AppColors.inProgress
                                          : AppColors.primary,
                                  foregroundColor: Colors.white,
                                ),
                                child: Row(
                                  mainAxisAlignment: MainAxisAlignment.center,
                                  children: [
                                    Icon(
                                      isCompleted
                                          ? Icons.check_circle_outline
                                          : Icons.play_arrow_outlined,
                                      size: 16,
                                    ),
                                    const SizedBox(width: 4),
                                    Text(
                                      isCompleted
                                          ? 'Completed'
                                          : isInProgress
                                              ? 'Continue'
                                              : 'Start Job',
                                      style: const TextStyle(
                                          fontSize: 13,
                                          fontWeight: FontWeight.bold),
                                    ),
                                  ],
                                ),
                              ),
                            ),
                          ],
                        ),
                      ],
                    ],
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
