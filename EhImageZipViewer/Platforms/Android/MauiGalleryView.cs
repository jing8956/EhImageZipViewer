using Android.Content;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.Widget;
using AndroidX.RecyclerView.Widget;
using EhImageZipViewer.Controls;
using _Microsoft.Android.Resource.Designer;
using Bumptech.Glide;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Bumptech.Glide.Request;
using Bumptech.Glide.Load;
using Bumptech.Glide.Request.Target;

namespace EhImageZipViewer.Platforms.Android;

public class MauiGalleryView : RecyclerView
{
    private readonly GalleryView _view;
    private readonly MauiGalleryViewAdapter _adapter;

    public MauiGalleryView(Context context, GalleryView view) : base(context)
    {
        _view = view;

        _adapter = new MauiGalleryViewAdapter(this);
        SetAdapter(_adapter);
    }

    public void UpdateImages()
    {
        var images = _view.Images;
        _adapter.SetImages(images);
    }

    private class MauiGalleryViewAdapter(MauiGalleryView view) : Adapter
    {
        private IReadOnlyList<ImageSource> _images = [];

        public void SetImages(IEnumerable<ImageSource>? images)
        {
            images ??= [];
            if(_images.Count == 0 && _images == images) return;

            if (_images is ObservableCollection<ImageSource> old)
            {
                old.CollectionChanged -= CollectionChanged;
            }

            _images = images as IReadOnlyList<ImageSource> ?? images.ToList();
            if (_images is ObservableCollection<ImageSource> oc)
            {
                oc.CollectionChanged += CollectionChanged;
            }

            NotifyDataSetChanged();
        }

        private void CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch(e.Action)
            {
                case NotifyCollectionChangedAction.Add: NotifyItemRangeInserted(e.NewStartingIndex, e.NewItems!.Count); break;
                case NotifyCollectionChangedAction.Reset: NotifyDataSetChanged(); break;
                default: throw new NotImplementedException();
            }
        }

        public override int ItemCount => _images.Count;

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var imageView = new AppCompatImageView(parent.Context!)
            {
                LayoutParameters = new ViewGroup.LayoutParams(
                    ViewGroup.LayoutParams.MatchParent,
                    ViewGroup.LayoutParams.WrapContent),
            };
            imageView.SetScaleType(ImageView.ScaleType.FitCenter);
            imageView.SetAdjustViewBounds(true);

            return new ViewHolder(imageView);
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var source = _images[position];
            var imageView = ((ViewHolder)holder).View;

            if (source.IsEmpty)
            {
                Glide.With(view).Clear(imageView);
                return;
            }

            var manager = Glide.With(view);
            RequestBuilder builder;
            switch (source)
            {
                case IFileImageSource fileSource:
                    builder = manager.Load(fileSource.File);
                    break;
                case IUriImageSource uriSource:
                    var uri = global::Android.Net.Uri.Parse(uriSource.Uri.ToString());
                    builder = manager.Load(uri);
                    break;
                // case IStreamImageSource streamSource:
                default: throw new ArgumentException($"Type '{source.GetType().FullName}' not support.");
            }

#pragma warning disable CS0618 // 类型或成员已过时
            _ = builder.Placeholder(ResourceConstant.Color.white)
                .Override(Target.SizeOriginal)
                .Format(DecodeFormat.PreferRgb565)
                .Into(imageView);
                // .Into(new DrawableImageViewTarget(imageView, true));
                // .Into(imageView).WaitForLayout(); // <-- BUG https://github.com/bumptech/glide/issues/3389
#pragma warning restore CS0618 // 类型或成员已过时
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_images is ObservableCollection<ImageSource> oc)
                {
                    oc.CollectionChanged -= CollectionChanged;
                }
            }

            base.Dispose(disposing);
        }

        public class ViewHolder(ImageView view) : RecyclerView.ViewHolder(view)
        {
            public ImageView View => view;
        }
    }
}
