import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:intl/intl.dart';
import '../core/routes/app_router.dart';
import '../core/theme/app_colors.dart';
import '../providers/work_order_provider.dart';
import '../widgets/add_note_modal.dart';
import '../widgets/job_timer.dart';
import '../widgets/notes_list.dart';
import '../widgets/photo_upload.dart';
import '../widgets/priority_badge.dart';
import '../widgets/status_badge.dart';

class JobExecutionScreen extends StatefulWidget {
  final String workOrderId;

  const JobExecutionScreen({super.key, required this.workOrderId});

  @override
  State<JobExecutionScreen> createState() => _JobExecutionScreenState();
}

class _JobExecutionScreenState extends State<JobExecutionScreen> {
  final GlobalKey<JobTimerState> _timerKey = GlobalKey<JobTimerState>();
  final GlobalKey<PhotoUploadState> _photoUploadKey =
      GlobalKey<PhotoUploadState>();
  final List<PhotoItem> _photos = [];
  bool _isCompletingJob = false;
  int _elapsedSeconds = 0;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<WorkOrderProvider>().fetchJobDetails(widget.workOrderId);
    });
  }

  void _openAddNoteModal() {
    AddNoteModal.show(
      context,
      onSave: (text) async {
        final success = await context
            .read<WorkOrderProvider>()
            .addNote(widget.workOrderId, text);
        return success;
      },
    );
  }

  String _formatElapsedTime(int totalSeconds) {
    final hours = totalSeconds ~/ 3600;
    final minutes = (totalSeconds % 3600) ~/ 60;
    final seconds = totalSeconds % 60;
    return '${hours.toString().padLeft(2, '0')}:${minutes.toString().padLeft(2, '0')}:${seconds.toString().padLeft(2, '0')}';
  }

  Future<void> _handleCompleteJob() async {
    if (_isCompletingJob) return;

    final timerState = _timerKey.currentState;
    final uploadedPhotos = _photos
        .where((photo) => photo.isUploaded && photo.fileKey != null)
        .toList();
    final uploadedPhoto = uploadedPhotos.isEmpty ? null : uploadedPhotos.first;
    final currentJob = context.read<WorkOrderProvider>().currentJob;

    setState(() {
      _isCompletingJob = true;
    });
    timerState?.pauseTimer();

    await Navigator.of(context).pushNamed(
      AppRouter.jobSignaturePath(widget.workOrderId),
      arguments: {
        'elapsedSeconds': timerState?.elapsedSeconds ?? _elapsedSeconds,
        'photoCount': uploadedPhotos.length,
        'noteCount': currentJob?.id == widget.workOrderId
            ? currentJob?.notes.length ?? 0
            : 0,
        'uploadedFileKey': uploadedPhoto?.fileKey,
        'uploadedPhotoName': uploadedPhoto?.name,
      },
    );

    if (mounted) {
      setState(() {
        _isCompletingJob = false;
      });
    }
  }

  Future<String?> _handleUploadPhoto(PhotoItem item) async {
    final fileKey = await context.read<WorkOrderProvider>().uploadEvidencePhoto(
          widget.workOrderId,
          item.bytes,
          item.name,
        );
    if (fileKey != null && mounted) {
      setState(() {
        item.fileKey = fileKey;
        item.isUploaded = true;
      });
    }
    return fileKey;
  }

  @override
  Widget build(BuildContext context) {
    final provider = context.watch<WorkOrderProvider>();
    final job = provider.currentJob?.id == widget.workOrderId
        ? provider.currentJob
        : null;
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Scaffold(
      appBar: AppBar(
        title: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              job?.workOrderNumber ?? 'Executing Job',
              style: const TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
            ),
            if (job != null)
              Text(
                job.status,
                style: TextStyle(
                  fontSize: 11,
                  color: AppColors.getStatusColor(job.status),
                  fontWeight: FontWeight.w600,
                ),
              ),
          ],
        ),
        actions: [
          Padding(
            padding: const EdgeInsets.symmetric(vertical: 12),
            child: Container(
              padding: const EdgeInsets.symmetric(horizontal: 8),
              decoration: BoxDecoration(
                color: isDark ? AppColors.darkCard : AppColors.primaryAccent,
                borderRadius: BorderRadius.circular(8),
              ),
              child: Row(
                children: [
                  const Icon(Icons.timer_outlined, size: 16),
                  const SizedBox(width: 4),
                  Text(
                    _formatElapsedTime(_elapsedSeconds),
                    style: const TextStyle(
                      fontSize: 12,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                ],
              ),
            ),
          ),
          IconButton(
            icon: const Icon(Icons.refresh),
            tooltip: 'Refresh',
            onPressed: () => provider.fetchJobDetails(widget.workOrderId),
          ),
        ],
      ),
      body: provider.isLoading && job == null
          ? const Center(child: CircularProgressIndicator())
          : job == null
              ? const Center(child: Text('Work order not found.'))
              : ListView(
                  padding: const EdgeInsets.all(16),
                  children: [
                    // Job Summary Card
                    Card(
                      shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(12)),
                      child: Padding(
                        padding: const EdgeInsets.all(14),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Row(
                              children: [
                                PriorityBadge(priority: job.priority),
                                const SizedBox(width: 8),
                                StatusBadge(status: job.status),
                              ],
                            ),
                            const SizedBox(height: 10),
                            Text(
                              job.title,
                              style: TextStyle(
                                fontSize: 16,
                                fontWeight: FontWeight.bold,
                                color: isDark
                                    ? AppColors.darkTextPrimary
                                    : AppColors.lightTextPrimary,
                              ),
                            ),
                            const SizedBox(height: 6),
                            Row(
                              children: [
                                Icon(Icons.location_on_outlined,
                                    size: 14,
                                    color: isDark
                                        ? AppColors.darkTextMuted
                                        : AppColors.lightTextMuted),
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
                            if (job.scheduledStartTime != null) ...[
                              const SizedBox(height: 6),
                              Row(
                                children: [
                                  Icon(Icons.access_time_outlined,
                                      size: 14,
                                      color: isDark
                                          ? AppColors.darkTextMuted
                                          : AppColors.lightTextMuted),
                                  const SizedBox(width: 4),
                                  Text(
                                    job.scheduledEndTime == null
                                        ? 'Starts ${DateFormat('hh:mm a').format(job.scheduledStartTime!)}'
                                        : '${DateFormat('hh:mm a').format(job.scheduledStartTime!)} - ${DateFormat('hh:mm a').format(job.scheduledEndTime!)}',
                                    style: TextStyle(
                                      fontSize: 13,
                                      color: isDark
                                          ? AppColors.darkTextSecondary
                                          : AppColors.lightTextSecondary,
                                    ),
                                  ),
                                ],
                              ),
                            ],
                          ],
                        ),
                      ),
                    ),
                    const SizedBox(height: 14),

                    JobTimer(
                      key: _timerKey,
                      initialElapsedSeconds: _elapsedSeconds,
                      onTick: (seconds) {
                        if (mounted) {
                          setState(() {
                            _elapsedSeconds = seconds;
                          });
                        }
                      },
                    ),
                    const SizedBox(height: 14),

                    // Field Work Notes Card
                    Card(
                      shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(12)),
                      child: Padding(
                        padding: const EdgeInsets.all(16),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Row(
                              mainAxisAlignment: MainAxisAlignment.spaceBetween,
                              children: [
                                Row(
                                  children: const [
                                    Icon(Icons.speaker_notes_outlined,
                                        color: AppColors.primary, size: 18),
                                    SizedBox(width: 8),
                                    Text(
                                      'Field Work Notes',
                                      style: TextStyle(
                                          fontSize: 15,
                                          fontWeight: FontWeight.bold),
                                    ),
                                  ],
                                ),
                                TextButton.icon(
                                  onPressed: _openAddNoteModal,
                                  icon: const Icon(Icons.add, size: 16),
                                  label: const Text('Add Note',
                                      style: TextStyle(fontSize: 12)),
                                ),
                              ],
                            ),
                            const Divider(height: 14),
                            NotesList(notes: job.notes),
                          ],
                        ),
                      ),
                    ),
                    const SizedBox(height: 14),

                    // Completion Evidence: Photos
                    Card(
                      shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(12)),
                      child: Padding(
                        padding: const EdgeInsets.all(16),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Row(
                              mainAxisAlignment: MainAxisAlignment.spaceBetween,
                              children: [
                                const Row(
                                  children: [
                                    Icon(Icons.photo_camera_outlined,
                                        color: AppColors.primary, size: 18),
                                    SizedBox(width: 8),
                                    Text(
                                      'Completion Evidence',
                                      style: TextStyle(
                                          fontSize: 15,
                                          fontWeight: FontWeight.bold),
                                    ),
                                  ],
                                ),
                                TextButton.icon(
                                  onPressed: () => _photoUploadKey.currentState
                                      ?.showImageSourceDialog(),
                                  icon: const Icon(Icons.add_a_photo_outlined,
                                      size: 16),
                                  label: const Text('Add Photo'),
                                ),
                              ],
                            ),
                            const SizedBox(height: 4),
                            Text(
                              'Take "after" photos showing completed repair.',
                              style: TextStyle(
                                fontSize: 12,
                                color: isDark
                                    ? AppColors.darkTextMuted
                                    : AppColors.lightTextMuted,
                              ),
                            ),
                            const Divider(height: 18),
                            PhotoUpload(
                              key: _photoUploadKey,
                              photos: _photos,
                              onAddPhoto: (item) {
                                setState(() {
                                  _photos.add(item);
                                });
                              },
                              onRemovePhoto: (id) {
                                setState(() {
                                  _photos.removeWhere((p) => p.id == id);
                                });
                              },
                              onUploadPhoto: _handleUploadPhoto,
                            ),
                          ],
                        ),
                      ),
                    ),
                    const SizedBox(height: 20),

                    // Complete Job Button
                    if (job.status.toLowerCase() != 'completed')
                      SizedBox(
                        height: 52,
                        child: ElevatedButton.icon(
                          onPressed:
                              _isCompletingJob ? null : _handleCompleteJob,
                          icon: _isCompletingJob
                              ? const SizedBox(
                                  width: 18,
                                  height: 18,
                                  child: CircularProgressIndicator(
                                      color: Colors.white, strokeWidth: 2),
                                )
                              : const Icon(Icons.check_circle_outline,
                                  size: 22),
                          label: Text(
                            _isCompletingJob
                                ? 'Navigating...'
                                : 'Complete Job & Capture Signature',
                            style: const TextStyle(
                                fontSize: 15, fontWeight: FontWeight.bold),
                          ),
                          style: ElevatedButton.styleFrom(
                            backgroundColor: AppColors.completed,
                            foregroundColor: Colors.white,
                            shape: RoundedRectangleBorder(
                                borderRadius: BorderRadius.circular(10)),
                          ),
                        ),
                      )
                    else
                      Container(
                        width: double.infinity,
                        padding: const EdgeInsets.all(16),
                        decoration: BoxDecoration(
                          color: AppColors.completedBg,
                          borderRadius: BorderRadius.circular(10),
                          border: Border.all(
                              color: AppColors.completed.withOpacity(0.3)),
                        ),
                        child: const Row(
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            Icon(Icons.check_circle,
                                color: AppColors.completed, size: 24),
                            SizedBox(width: 10),
                            Text(
                              'Job Completed Successfully',
                              style: TextStyle(
                                fontWeight: FontWeight.bold,
                                color: AppColors.completed,
                                fontSize: 15,
                              ),
                            ),
                          ],
                        ),
                      ),
                    const SizedBox(height: 24),
                  ],
                ),
    );
  }
}
