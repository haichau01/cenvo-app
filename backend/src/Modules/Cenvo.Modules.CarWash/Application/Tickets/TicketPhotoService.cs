namespace Cenvo.Modules.CarWash.Application.Tickets;

public record PhotoFile(Stream Content, string ContentType);

/// <summary>Vehicle condition photos. Files are checked by their bytes; access always goes through the (branch-filtered) ticket.</summary>
public class TicketPhotoService(AppDbContext db, IPhotoStorage storage, IWashNotifier notifier, ICurrentUser user)
{
    public async Task<TicketPhotoDto> Upload(Guid ticketId, Stream content, long length, CancellationToken ct)
    {
        if (length <= 0) throw new BusinessException(CarWashErrors.NoPhoto);
        if (length > ImageFormat.MaxBytes) throw new BusinessException(CarWashErrors.PhotoTooLarge);

        var ticket = await db.LoadTicket(ticketId, ct);
        var (format, buffered) = await Inspect(content, length, ct);
        var key = await storage.Save(buffered, format.Extension, ct);
        var photo = new WashTicketPhoto
        {
            StorageKey = key, ContentType = format.ContentType, SizeBytes = buffered.Length, TakenAt = DateTime.UtcNow, TakenBy = user.UserId,
        };
        try
        {
            ticket.AddPhoto(photo);
            await db.SaveChangesAsync(ct);
        }
        catch
        {
            storage.Delete(key); // no record → no orphan file
            throw;
        }
        await notifier.TicketsChanged(ticket.BranchId, ct);
        return new TicketPhotoDto(photo.Id, photo.TakenAt);
    }

    public async Task<PhotoFile> Open(Guid ticketId, Guid photoId, CancellationToken ct)
    {
        var photo = await db.Set<WashTicket>().AsNoTracking()
                        .Where(t => t.Id == ticketId)
                        .SelectMany(t => t.Photos)
                        .Where(p => p.Id == photoId)
                        .Select(p => new { p.StorageKey, p.ContentType })
                        .FirstOrDefaultAsync(ct)
                    ?? throw new NotFoundException("ảnh", photoId);
        var stream = storage.Open(photo.StorageKey) ?? throw new NotFoundException("tệp ảnh", photoId);
        return new PhotoFile(stream, photo.ContentType);
    }

    public async Task Delete(Guid ticketId, Guid photoId, CancellationToken ct)
    {
        var ticket = await db.LoadTicket(ticketId, ct);
        var photo = ticket.RemovePhoto(photoId);
        await db.SaveChangesAsync(ct);
        storage.Delete(photo.StorageKey);
        await notifier.TicketsChanged(ticket.BranchId, ct);
    }

    /// <summary>Copies the upload into memory (≤ 5 MB) and recognises the image from its first bytes.</summary>
    private static async Task<(ImageFormat.Format Format, MemoryStream Buffered)> Inspect(Stream content, long length, CancellationToken ct)
    {
        var buffered = new MemoryStream((int)length);
        await content.CopyToAsync(buffered, ct);
        if (buffered.Length > ImageFormat.MaxBytes) throw new BusinessException(CarWashErrors.PhotoTooLarge);
        var format = Detect(buffered) ?? throw new BusinessException(CarWashErrors.PhotoNotImage);
        buffered.Position = 0;
        return (format, buffered);
    }

    private static ImageFormat.Format? Detect(MemoryStream buffered) =>
        ImageFormat.Detect(buffered.GetBuffer().AsSpan(0, (int)Math.Min(buffered.Length, ImageFormat.HeaderLength)));
}
